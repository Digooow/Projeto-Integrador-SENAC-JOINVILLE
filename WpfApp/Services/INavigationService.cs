namespace WpfApp.Services
{
    public interface INavigationService
    {
        void NavigateTo<TViewModel>() where TViewModel : class;
    }
}