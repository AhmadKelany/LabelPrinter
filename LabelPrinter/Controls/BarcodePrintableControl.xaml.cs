using System.Windows.Controls;
using LabelPrinter.Models;

namespace LabelPrinter.Controls
{
    public partial class BarcodePrintableControl : UserControl
    {
        public BarcodePrintableControl()
        {
            InitializeComponent();
            BarcodeTypeCombo.ItemsSource = Enum.GetValues<BarcodeImageType>();
        }
    }
}
