using System.Windows;
using System.Windows.Controls;
using LabelPrinter.Models;

namespace LabelPrinter.Controls
{
    public partial class TextPrintableControl : UserControl
    {
        public TextPrintableControl()
        {
            InitializeComponent();
            HorizontalAlignmentCombo.ItemsSource = new[]
            {
                TextAlignment.Left,
                TextAlignment.Center,
                TextAlignment.Right,
                TextAlignment.Justify
            };
            VerticalAlignmentCombo.ItemsSource = Enum.GetValues<VerticalTextAlignment>();
        }
    }
}
