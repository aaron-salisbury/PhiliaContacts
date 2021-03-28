using PhiliaContacts.App.ViewModels;
using Windows.UI.Xaml.Controls;

namespace PhiliaContacts.App.Views
{
    public sealed partial class IntroductionPage : Page
    {
        public IntroductionPage()
        {
            this.InitializeComponent();
            DataContext = ViewModelLocator.Current.IntroductionViewModel;
        }
    }
}
