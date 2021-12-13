using PhiliaContacts.App.Base.Extensions;

namespace PhiliaContacts.App.ViewModels
{
    public class IntroductionViewModel : BaseViewModel
    {
        private string _appDisplayName;

        public string AppDisplayName
        {
            get => _appDisplayName;
            set => SetProperty(ref _appDisplayName, value);
        }

        public IntroductionViewModel()
        {
            AppDisplayName = "AppDisplayName".GetLocalized();
        }
    }
}
