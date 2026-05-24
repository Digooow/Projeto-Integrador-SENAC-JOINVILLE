// Arquivo mantido para compatibilidade; movido para ViewModels namespace. Remover se não for necessário.
namespace WpfApp
{
    // RelayCommand permanece no namespace raiz para uso simples
    public class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly System.Action _execute;
        private readonly System.Func<bool>? _canExecute;

        public RelayCommand(System.Action execute, System.Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public event System.EventHandler? CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }
    }
}
