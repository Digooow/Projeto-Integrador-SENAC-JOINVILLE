using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace Cantina.Wpf.Services;

public sealed record AppUpdateCheckResult(
    bool UpdateAvailable,
    string CurrentVersion,
    string LatestVersion,
    string ReleaseTitle,
    string ReleaseNotes,
    string DownloadUrl,
    string HtmlReleaseUrl,
    string StatusMessage);

public sealed class AppUpdateService : IDisposable
{
    private readonly HttpClient _http;
    private readonly CantinaWpfSettings _settings;

    public AppUpdateService(CantinaWpfSettings settings)
    {
        _settings = settings;
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CantinaAlamoDesktop", CurrentVersion));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public static string CurrentVersion
    {
        get
        {
            var infoVersion = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(infoVersion))
            {
                var clean = infoVersion.Split('+')[0].Trim();
                if (!string.IsNullOrWhiteSpace(clean))
                    return clean;
            }

            var asmVersion = Assembly.GetEntryAssembly()?.GetName().Version;
            return asmVersion is not null
                ? $"{asmVersion.Major}.{asmVersion.Minor}.{Math.Max(0, asmVersion.Build)}"
                : "1.2.0";
        }
    }

    public static string LocalInstallDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CantinaAlamo");

    public static bool IsInstalledInLocalAppData =>
        AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)
            .StartsWith(LocalInstallDirectory.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

    public async Task<AppUpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        var current = CurrentVersion;
        var repoUrl = (_settings.UpdateRepositoryUrl ?? "https://github.com/Digooow/Projeto-Integrador-SENAC-JOINVILLE").Trim().TrimEnd('/');
        var apiEndpoint = BuildGithubLatestReleaseApiUrl(repoUrl);

        try
        {
            using var response = await _http.GetAsync(apiEndpoint, ct);
            if (!response.IsSuccessStatusCode)
            {
                return new AppUpdateCheckResult(
                    UpdateAvailable: false,
                    CurrentVersion: current,
                    LatestVersion: current,
                    ReleaseTitle: $"Cantina ALAMO v{current}",
                    ReleaseNotes: "Canal de releases GitHub pronto. Publique uma tag v* para disponibilizar um novo instalador OTA.",
                    DownloadUrl: $"{repoUrl}/releases/latest",
                    HtmlReleaseUrl: $"{repoUrl}/releases",
                    StatusMessage: $"Você está na versão mais recente (v{current}).");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? current : current;
            var latestClean = tagName.TrimStart('v', 'V').Split('-')[0].Trim();
            var title = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? tagName : tagName;
            var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? string.Empty : string.Empty;
            var htmlUrl = root.TryGetProperty("html_url", out var htmlProp) ? htmlProp.GetString() ?? $"{repoUrl}/releases" : $"{repoUrl}/releases";

            string downloadUrl = htmlUrl;
            if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsProp.EnumerateArray())
                {
                    var assetName = asset.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                    var browserUrl = asset.TryGetProperty("browser_download_url", out var b) ? b.GetString() ?? string.Empty : string.Empty;

                    if (assetName.EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase) ||
                        assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = browserUrl;
                        break;
                    }
                }
            }

            var isNewer = IsNewerVersion(current, latestClean);
            var statusMsg = isNewer
                ? $"Nova atualização disponível: v{latestClean} (instalada: v{current})!"
                : $"Programa atualizado na última versão (v{current}).";

            return new AppUpdateCheckResult(
                UpdateAvailable: isNewer,
                CurrentVersion: current,
                LatestVersion: latestClean,
                ReleaseTitle: title,
                ReleaseNotes: body,
                DownloadUrl: downloadUrl,
                HtmlReleaseUrl: htmlUrl,
                StatusMessage: statusMsg);
        }
        catch (Exception ex)
        {
            return new AppUpdateCheckResult(
                UpdateAvailable: false,
                CurrentVersion: current,
                LatestVersion: current,
                ReleaseTitle: $"v{current}",
                ReleaseNotes: string.Empty,
                DownloadUrl: $"{repoUrl}/releases",
                HtmlReleaseUrl: $"{repoUrl}/releases",
                StatusMessage: $"Não foi possível consultar o servidor de atualizações ({ex.Message}).");
        }
    }

    public async Task<string> DownloadAndApplyUpdateAsync(AppUpdateCheckResult checkResult, CancellationToken ct = default)
    {
        // 1. Se foi instalado via Velopack (Update.exe presente no diretório pai), delega para o Velopack Update.exe
        var parentDir = Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))?.FullName;
        if (!string.IsNullOrWhiteSpace(parentDir))
        {
            var velopackUpdater = Path.Combine(parentDir, "Update.exe");
            if (File.Exists(velopackUpdater))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = velopackUpdater,
                    Arguments = $"--update \"{_settings.UpdateRepositoryUrl}/releases/latest/download\"",
                    UseShellExecute = true
                });
                return "Atualizador Velopack iniciado em segundo plano.";
            }
        }

        // 2. Se o asset na Release for um instalador .exe direto, baixa em %LocalAppData%\CantinaAlamo\Updates e executa
        if (!string.IsNullOrWhiteSpace(checkResult.DownloadUrl) &&
            checkResult.DownloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            var updatesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CantinaAlamo",
                "Updates");
            Directory.CreateDirectory(updatesDir);

            var installerPath = Path.Combine(updatesDir, $"CantinaAlamo-v{checkResult.LatestVersion}-Setup.exe");
            using var response = await _http.GetAsync(checkResult.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using (var fs = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs, ct);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });

            return $"Instalador v{checkResult.LatestVersion} baixado e iniciado com sucesso!";
        }

        // 3. Fallback: Abre a página de Releases oficial no navegador
        Process.Start(new ProcessStartInfo
        {
            FileName = checkResult.HtmlReleaseUrl,
            UseShellExecute = true
        });
        return "Página oficial de download aberta no navegador.";
    }

    public static string InstallToLocalMachineAndCreateShortcuts()
    {
        var sourceDir = AppContext.BaseDirectory;
        var targetDir = LocalInstallDirectory;
        Directory.CreateDirectory(targetDir);

        // Se estiver rodando fora da pasta de instalação local, copia os binários para %LocalAppData%\Programs\CantinaAlamo
        if (!IsInstalledInLocalAppData)
        {
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                    continue;

                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, overwrite: true);
            }

            var wwwrootSource = Path.Combine(sourceDir, "wwwroot");
            if (Directory.Exists(wwwrootSource))
            {
                CopyDirectoryRecursive(wwwrootSource, Path.Combine(targetDir, "wwwroot"));
            }
        }

        var exeName = Process.GetCurrentProcess().MainModule?.FileName;
        var exeFileName = !string.IsNullOrWhiteSpace(exeName) ? Path.GetFileName(exeName) : "CantinaAlamo.exe";
        var targetExePath = Path.Combine(targetDir, exeFileName);
        if (!File.Exists(targetExePath) && !string.IsNullOrWhiteSpace(exeName))
        {
            targetExePath = exeName;
        }

        var desktopShortcut = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "Cantina ALAMO — Admin.lnk");

        var startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            "Cantina ALAMO SENAC");
        Directory.CreateDirectory(startMenuDir);
        var startMenuShortcut = Path.Combine(startMenuDir, "Cantina ALAMO — Admin.lnk");

        CreateWindowsShortcut(desktopShortcut, targetExePath, targetDir, "Painel Gerencial DesperdícioZero — Cantina ALAMO (SENAC)");
        CreateWindowsShortcut(startMenuShortcut, targetExePath, targetDir, "Painel Gerencial DesperdícioZero — Cantina ALAMO (SENAC)");

        return targetDir;
    }

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSub = Path.Combine(targetDir, Path.GetFileName(subDir));
            CopyDirectoryRecursive(subDir, destSub);
        }
    }

    private static void CreateWindowsShortcut(string shortcutPath, string targetExe, string workingDir, string description)
    {
        var escapedShortcut = shortcutPath.Replace("'", "''");
        var escapedTarget = targetExe.Replace("'", "''");
        var escapedWorkDir = workingDir.Replace("'", "''");
        var escapedDesc = description.Replace("'", "''");

        var psScript =
            $"$ws = New-Object -ComObject WScript.Shell; " +
            $"$s = $ws.CreateShortcut('{escapedShortcut}'); " +
            $"$s.TargetPath = '{escapedTarget}'; " +
            $"$s.WorkingDirectory = '{escapedWorkDir}'; " +
            $"$s.Description = '{escapedDesc}'; " +
            $"$s.Save()";

        using var proc = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"{psScript}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        });
        proc?.WaitForExit(4000);
    }

    private static string BuildGithubLatestReleaseApiUrl(string repoUrl)
    {
        if (Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri) &&
            uri.Host.Contains("github.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                return $"https://api.github.com/repos/{segments[0]}/{segments[1]}/releases/latest";
            }
        }

        return "https://api.github.com/repos/Digooow/Projeto-Integrador-SENAC-JOINVILLE/releases/latest";
    }

    private static bool IsNewerVersion(string currentVersion, string latestVersion)
    {
        if (Version.TryParse(NormalizeThreePartVersion(currentVersion), out var current) &&
            Version.TryParse(NormalizeThreePartVersion(latestVersion), out var latest))
        {
            return latest > current;
        }

        return !string.Equals(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeThreePartVersion(string raw)
    {
        var clean = (raw ?? "1.0.0").Trim().TrimStart('v', 'V').Split('-', '+')[0];
        var parts = clean.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            1 => $"{parts[0]}.0.0",
            2 => $"{parts[0]}.{parts[1]}.0",
            _ => string.Join('.', parts.Take(3))
        };
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}

