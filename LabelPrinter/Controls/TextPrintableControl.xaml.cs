using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LabelPrinter.Models;

namespace LabelPrinter.Controls
{
    public partial class TextPrintableControl : UserControl
    {
        public TextPrintableControl()
        {
            InitializeComponent();

            FontFamilyCombo.ItemsSource = Fonts.SystemFontFamilies
                .OrderBy(font => font.Source, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

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
