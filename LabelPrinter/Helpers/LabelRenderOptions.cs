using System.Windows.Media;
using LabelPrinter.Models;

namespace LabelPrinter.Helpers
{
    public sealed class LabelRenderOptions
    {
        public bool DrawLabelBackground { get; set; } = true;
        public bool DrawLabelBorder { get; set; } = false;
        public bool ShowValidationErrors { get; set; } = true;
        public double PixelsPerDip { get; set; } = 1.0;
        public double LabelCornerRadiusDip { get; set; }
        public int BarcodeQuietZoneModules { get; set; } = 2;
        public PrintableObject? HighlightedItem { get; set; }
        public Brush HighlightBrush { get; set; } = new SolidColorBrush(Color.FromRgb(250, 244, 232));
    }
}
