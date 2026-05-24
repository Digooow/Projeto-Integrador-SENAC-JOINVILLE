using System.Windows.Input;
using WpfApp;

namespace WpfApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private string _title = "Projeto Integrador - WPF";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ICommand ChangeTitleCommand { get; }

        public MainViewModel()
        {
            ChangeTitleCommand = new RelayCommand(() => Title = "Título alterado pelo botão");
        }
    }
}