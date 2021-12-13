using Microsoft.Extensions.DependencyInjection;
using PhiliaContacts.App.ViewModels;
using Windows.UI.Xaml.Controls;

namespace PhiliaContacts.App
{
    public sealed partial class ShellPage : Page
    {
        public ShellViewModel ViewModel => (ShellViewModel)DataContext;

        public ShellPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService<ShellViewModel>();
            ViewModel.Initialize(shellFrame, navigationView, KeyboardAccelerators);

            navigationView.IsPaneOpen = false;
        }
    }
}
