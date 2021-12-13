using PhiliaContacts.App.ViewModels;
using Windows.UI.Xaml.Controls;

namespace PhiliaContacts.App.Views
{
    public sealed partial class LogPage : Page
    {
        public LogViewModel ViewModel { get; } = new LogViewModel();

        public LogPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
