
using System.Globalization;
using System.Threading;
using System.Windows;
using Projeto_Integrador_SENAC.Services;
using Projeto_Integrador_SENAC.Views;

namespace Projeto_Integrador_SENAC
{
    public partial class App : Application
    {
        public static IProdutoService Service { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var cultura = new CultureInfo("pt-BR");

            Thread.CurrentThread.CurrentCulture = cultura;
            Thread.CurrentThread.CurrentUICulture = cultura;

            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;

            base.OnStartup(e);

            Service = new ProdutoService();

            var main = new MainWindow(Service);
            main.Show();
        }
    }
}
