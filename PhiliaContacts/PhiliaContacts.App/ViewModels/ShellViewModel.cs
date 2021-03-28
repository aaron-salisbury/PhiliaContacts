using PhiliaContacts.Core;
using PhiliaContacts.Core.Base;
using Windows.UI.Xaml;

namespace PhiliaContacts.App.ViewModels
{
    public class ShellViewModel : BaseNavigableViewModel
    {
        public static Visibility IsDebug
        {
#if DEBUG
            get { return Visibility.Visible; }
#else
            get { return Visibility.Collapsed; }
#endif
        }

        public AppLogger AppLogger { get; set; }
        public Manager Manager { get; set; }

        public ShellViewModel()
        {
            AppLogger = new AppLogger();
            Manager = new Manager(AppLogger);
        }
    }
}
