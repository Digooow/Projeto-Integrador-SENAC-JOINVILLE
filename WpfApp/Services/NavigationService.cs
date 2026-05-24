using System;
using System.Collections.Generic;
using System.Windows.Controls;
using WpfApp.ViewModels;
using WpfApp.Views;

namespace WpfApp.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Dictionary<Type, UserControl> _views = new();
        private readonly ContentControl _host;

        public NavigationService(ContentControl host)
        {
            _host = host;
            // Register views
            _views[typeof(MainViewModel)] = new MainView();
        }

        public void NavigateTo<TViewModel>() where TViewModel : class
        {
            var type = typeof(TViewModel);
            if (_views.TryGetValue(type, out var view))
            {
                _host.Content = view;
            }
        }
    }
}