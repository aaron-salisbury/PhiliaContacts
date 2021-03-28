using PhiliaContacts.App.ViewModels;
using Windows.UI.Xaml.Controls;

namespace PhiliaContacts.App.Views
{
    public sealed partial class LogPage : Page
    {
        public LogPage()
        {
            InitializeComponent();
            DataContext = ViewModelLocator.Current.LogViewModel;
        }
    }
}
