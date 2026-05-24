using System.Windows;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Setup navigation
            var navigation = new Services.NavigationService(MainHost);
            var vm = new ViewModels.MainViewModel();
            DataContext = vm;
            navigation.NavigateTo<ViewModels.MainViewModel>();
        }
    }
}
