using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Feeder.Common.Controls
{
    public class MultiStateButtonBase: Button
    {
        public ImageSource ImageSource
        {
            get { return GetValue(SourceProperty) as ImageSource; }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
          DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(MultiStateButtonBase));

        public string BackgroundColor
        {
            get { return GetValue(SourceProperty) as string; }
            set { SetValue(SourceProperty,value); }
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof (string), typeof (MultiStateButtonBase));
    }
}
