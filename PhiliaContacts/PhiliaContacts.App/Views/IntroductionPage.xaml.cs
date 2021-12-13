using PhiliaContacts.App.ViewModels;
using Windows.UI.Xaml.Controls;

namespace PhiliaContacts.App.Views
{
    public sealed partial class IntroductionPage : Page
    {
        public IntroductionViewModel ViewModel { get; } = new IntroductionViewModel();

        public IntroductionPage()
        {
            this.InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
