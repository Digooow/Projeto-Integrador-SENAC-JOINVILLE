
using System.Windows;
using Projeto_Integrador_SENAC.ViewModels;
using Projeto_Integrador_SENAC.Services;

namespace Projeto_Integrador_SENAC.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(IProdutoService service)
        {
            InitializeComponent();
            DataContext = new MainViewModel(service);
        }
    }
}
