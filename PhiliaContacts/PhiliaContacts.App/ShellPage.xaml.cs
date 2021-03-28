using PhiliaContacts.App.ViewModels;
using Windows.UI.Xaml.Controls;

namespace PhiliaContacts.App
{
    public sealed partial class ShellPage : Page
    {
        private ShellViewModel ViewModel
        {
            get => ViewModelLocator.Current.ShellViewModel;
        }

        public ShellPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
            ViewModel.Initialize(shellFrame, navigationView, KeyboardAccelerators);

            navigationView.IsPaneOpen = false;
        }
    }
}
