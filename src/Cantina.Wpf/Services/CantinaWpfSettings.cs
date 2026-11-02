using System.IO;
using System.Text.Json;

namespace Cantina.Wpf.Services;

public sealed class CantinaWpfSettings
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5180";
    public string AdminApiKey { get; set; } = string.Empty;
    public bool AutoStartLocalServer { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 6;

    private static string ConfigPath =>
        Path.Combine(AppContext.BaseDirectory, "appsettings.wpf.json");

    public static CantinaWpfSettings Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var loaded = JsonSerializer.Deserialize<CantinaWpfSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loaded is not null && !string.IsNullOrWhiteSpace(loaded.ApiBaseUrl))
                {
                    loaded.ApiBaseUrl = loaded.ApiBaseUrl.TrimEnd('/');
                    return loaded;
                }
            }
        }
        catch
        {
            // Usa configuração padrão resiliente caso o JSON local esteja inválido
        }

        return new CantinaWpfSettings();
    }

    public void Save()
    {
        ApiBaseUrl = (ApiBaseUrl ?? "http://localhost:5180").Trim().TrimEnd('/');
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}

