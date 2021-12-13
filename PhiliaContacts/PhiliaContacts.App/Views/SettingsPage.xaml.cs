using PhiliaContacts.App.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace PhiliaContacts.App.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; } = new SettingsViewModel();

        public SettingsPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.InitializeAsync();
        }
    }
}
