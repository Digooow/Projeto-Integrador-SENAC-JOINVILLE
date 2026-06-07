using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Projeto_Integrador_SENAC.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? nome = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
        }
    }
}
