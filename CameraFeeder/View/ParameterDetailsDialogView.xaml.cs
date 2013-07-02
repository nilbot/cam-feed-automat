using System.Windows.Controls;
using Feeder.ViewModel;

namespace Feeder.View
{
    public partial class ParameterDetailsDialogView : UserControl
    {
        public ParameterDetailsDialogView()
        {
            DataContext = new ParameterDetailsDialogViewModel();
            InitializeComponent();
        }
    }
}
