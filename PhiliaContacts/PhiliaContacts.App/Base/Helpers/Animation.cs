using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace PhiliaContacts.App.Base.Helpers
{
    internal class Animation
    {
        internal static void AnimateOpacity(DependencyObject target)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                Duration = new Duration(new TimeSpan(TimeSpan.TicksPerSecond)),
                BeginTime = new TimeSpan(TimeSpan.TicksPerSecond / 2),
                To = 1,
                From = 0,
                AutoReverse = true
            };

            Animate(target, animation, "Opacity");
        }

        internal static void AnimateSuccessFailColor(DependencyObject target, bool isSuccessful)
        {
            Color toColor = isSuccessful
                ? PlatformShim.BrushConverterConvertFrom("#2ecc71").Color
                : Colors.Red;

            ColorAnimation animation = new ColorAnimation
            {
                Duration = new Duration(new TimeSpan(TimeSpan.TicksPerSecond / 2)),
                To = toColor,
                From = null,
                AutoReverse = true
            };

            Animate(target, animation, "Color");
        }

        private static void Animate(DependencyObject target, Timeline element, string path)
        {
            Storyboard.SetTarget(element, target);
            Storyboard.SetTargetProperty(element, path);

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(element);
            storyboard.Begin();
        }
    }
}
