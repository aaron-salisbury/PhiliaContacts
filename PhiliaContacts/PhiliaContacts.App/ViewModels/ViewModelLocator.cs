using GalaSoft.MvvmLight.Ioc;
using PhiliaContacts.App.Base.Services;
using PhiliaContacts.App.Views;

namespace PhiliaContacts.App.ViewModels
{
    [Windows.UI.Xaml.Data.Bindable]
    public class ViewModelLocator
    {
        private static ViewModelLocator _current;
        public static ViewModelLocator Current => _current ?? (_current = new ViewModelLocator());

        public ViewModelLocator()
        {
            SimpleIoc.Default.Register(() => new NavigationServiceEx());
            SimpleIoc.Default.Register<ShellViewModel>();
            Register<SettingsViewModel, SettingsPage>();
            Register<IntroductionViewModel, IntroductionPage>();
            Register<LogViewModel, LogPage>();
            Register<ContactsViewModel, ContactsPage>();
        }

        public NavigationServiceEx NavigationService => SimpleIoc.Default.GetInstance<NavigationServiceEx>();
        public ShellViewModel ShellViewModel => SimpleIoc.Default.GetInstance<ShellViewModel>();
        public SettingsViewModel SettingsViewModel => SimpleIoc.Default.GetInstance<SettingsViewModel>();
        public IntroductionViewModel IntroductionViewModel => SimpleIoc.Default.GetInstance<IntroductionViewModel>();
        public LogViewModel LogViewModel => SimpleIoc.Default.GetInstance<LogViewModel>();
        public ContactsViewModel ContactsViewModel => SimpleIoc.Default.GetInstance<ContactsViewModel>();

        public void Register<VM, V>() where VM : class
        {
            SimpleIoc.Default.Register<VM>();

            NavigationService.Configure(typeof(VM).FullName, typeof(V));
        }
    }
}
