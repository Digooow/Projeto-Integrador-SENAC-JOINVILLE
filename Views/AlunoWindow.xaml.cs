using System.Windows;
using Projeto_Integrador_SENAC.ViewModels;

namespace Projeto_Integrador_SENAC.Views
{
    public partial class AlunoWindow : Window
    {
        public AlunoWindow(AlunoViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}