using System.Windows.Controls;
using LabelPrinter.Models;

namespace LabelPrinter.Controls
{
    public partial class RectanglePrintableControl : UserControl
    {
        public RectanglePrintableControl()
        {
            InitializeComponent();
            LineStyleCombo.ItemsSource = Enum.GetValues<LineStyle>();
        }
    }
}
