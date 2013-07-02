using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Feeder.Common.Behaviour
{
    public class MouseSelectBehaviour
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached("Command",
                                                                                                        typeof (ICommand
                                                                                                            ),
                                                                                                        typeof (
                                                                                                            MouseSelectBehaviour
                                                                                                            ),
                                                                                                        new PropertyMetadata
                                                                                                            (PropertyChangedCallback));

        public static void PropertyChangedCallback(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
        {
            var _selector = (Selector) depObj;
            if (_selector != null)
                _selector.SelectionChanged += selectionChanged;
        }

        public static ICommand GetCommand(UIElement element)
        {
            return (ICommand) element.GetValue(CommandProperty);
        }

        public static void SetCommand(UIElement element, ICommand command)
        {
            element.SetValue(CommandProperty, command);
        }

        private static void selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var _selector = (Selector) sender;
            if (_selector != null)
            {
                var _command = _selector.GetValue(CommandProperty) as ICommand;
                if (_command != null)
                    _command.Execute(_selector.SelectedItem);
            }
        }
    }
}
