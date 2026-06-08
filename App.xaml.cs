using System.Globalization;
using System.Threading;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_Integrador_SENAC.Data;
using Projeto_Integrador_SENAC.Services;
using Projeto_Integrador_SENAC.ViewModels;
using Projeto_Integrador_SENAC.Views;

namespace Projeto_Integrador_SENAC
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Configuração de cultura
            var cultura = new CultureInfo("pt-BR");
            Thread.CurrentThread.CurrentCulture = cultura;
            Thread.CurrentThread.CurrentUICulture = cultura;
            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;

            // Configuração do container de DI
            var services = new ServiceCollection();

            // String de conexão com MariaDB (ajuste para seu ambiente)
            string connectionString = "Server=localhost;Database=ProjetoIntegradorSENAC;User Id=root;Password=;";

            services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            // Registrar serviços
            services.AddScoped<IProdutoService, ProdutoService>();
            services.AddScoped<IAlunoService, AlunoService>();

            // Registrar ViewModels (se usar DI nas Views)
            services.AddTransient<MainViewModel>();
            services.AddTransient<AlunoViewModel>();

            // Registrar Views (para injeção do ViewModel via construtor)
            services.AddTransient<MainWindow>();
            services.AddTransient<AlunoWindow>();

            services.AddScoped<IAlunoService, AlunoService>();
            services.AddTransient<AlunoViewModel>();
            services.AddTransient<AlunoWindow>();

            ServiceProvider = services.BuildServiceProvider();

            base.OnStartup(e);

            // Obter a MainWindow com suas dependências resolvidas
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (ServiceProvider is IDisposable disposable)
                disposable.Dispose();
            base.OnExit(e);
        }
    }
}