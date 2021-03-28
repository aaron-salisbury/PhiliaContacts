using PhiliaContacts.App.Base.Helpers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PhiliaContacts.App.Base.Controls
{
    public sealed partial class MyAppBar : UserControl
    {
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(MyAppBar.IsActive), typeof(bool), typeof(MyAppBar), null);

        public object AppBarButtonsPanel
        {
            get { return GetValue(AppBarButtonsPanelProperty); }
            set { SetValue(AppBarButtonsPanelProperty, value); }
        }

        public static readonly DependencyProperty AppBarButtonsPanelProperty =
            DependencyProperty.Register(nameof(MyAppBar.AppBarButtonsPanel), typeof(object), typeof(MyAppBar), null);

        public MyAppBar()
        {
            this.InitializeComponent();
        }

        public void AnimateSucess()
        {
            Animation.AnimateOpacity(SuccessIcon);
        }

        public void AnimateFailure()
        {
            Animation.AnimateOpacity(FailureIcon);
        }
    }
}
