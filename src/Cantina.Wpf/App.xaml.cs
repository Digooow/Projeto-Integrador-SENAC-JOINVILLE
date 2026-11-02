using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using Cantina.Api;
using Cantina.Wpf.Services;
using Cantina.Wpf.ViewModels;
using Cantina.Wpf.Views;
using Microsoft.AspNetCore.Builder;

namespace Cantina.Wpf;

public partial class App : System.Windows.Application
{
    private static bool _languageMetadataConfigured;

    private WebApplication? _embeddedServer;
    private CantinaApiClient? _apiClient;
    private MainDashboardViewModel? _viewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        ConfigurePortugueseCulture();
        base.OnStartup(e);

        var settings = CantinaWpfSettings.Load();
        _apiClient = new CantinaApiClient(settings);

        var online = await _apiClient.CheckHealthAsync();
        if (!online && settings.AutoStartLocalServer && IsLocalAddress(settings.ApiBaseUrl))
        {
            try
            {
                _embeddedServer = CantinaApiHost.CreateWebApplication([], settings.ApiBaseUrl);
                await CantinaApiHost.InitializeDatabaseAsync(_embeddedServer);
                await _embeddedServer.StartAsync();
            }
            catch
            {
                // Se a porta já estiver em uso ou indisponível, o painel exibirá o status de conexão para ajuste
            }
        }

        _viewModel = new MainDashboardViewModel(_apiClient, settings);
        var mainWindow = new MainWindow(_viewModel);
        MainWindow = mainWindow;
        mainWindow.Show();

        await _viewModel.InicializarAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _viewModel?.Dispose();
        _apiClient?.Dispose();

        if (_embeddedServer is not null)
        {
            try
            {
                await _embeddedServer.StopAsync();
                await _embeddedServer.DisposeAsync();
            }
            catch
            {
                // Encerramento limpo
            }
        }

        base.OnExit(e);
    }

    private static void ConfigurePortugueseCulture()
    {
        var ptBr = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = ptBr;
        CultureInfo.DefaultThreadCurrentUICulture = ptBr;

        if (!_languageMetadataConfigured)
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(ptBr.IetfLanguageTag)));
            _languageMetadataConfigured = true;
        }
    }

    private static bool IsLocalAddress(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
               || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }
}
