
using Projeto_Integrador_SENAC.Models;
using Projeto_Integrador_SENAC.Services;
using Projeto_Integrador_SENAC.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Projeto_Integrador_SENAC
{

    public partial class CadastroWindow : Window
    {
        public CadastroWindow(IProdutoService service)
        {
            InitializeComponent();
            DataContext = new CadastroViewModel(service);
        }
    }

}
