using System;
using System.Windows;
using System.Windows.Media;

namespace LabelPrinter.Models
{
    public class TextPrintable : PrintableObject
    {
        private string _text = string.Empty;
        private string _fontFamily = "Segoe UI";
        private double _fontSize = 12.0;
        private FontStyle _fontStyle = FontStyles.Normal;
        private FontWeight _fontWeight = FontWeights.Normal;
        private Brush _foreground = Brushes.Black;

        public string Text { get => _text; set => SetProperty(ref _text, value); }
        public string FontFamily { get => _fontFamily; set => SetProperty(ref _fontFamily, value); }
        public double FontSize { get => _fontSize; set => SetProperty(ref _fontSize, value); }
        public FontStyle FontStyle { get => _fontStyle; set => SetProperty(ref _fontStyle, value); }
        public FontWeight FontWeight { get => _fontWeight; set => SetProperty(ref _fontWeight, value); }
        public Brush Foreground { get => _foreground; set => SetProperty(ref _foreground, value); }
        // Horizontal text alignment
        public System.Windows.TextAlignment HorizontalTextAlignment { get; set; } = System.Windows.TextAlignment.Left;

        // Vertical text alignment within the bounding box
        public VerticalTextAlignment VerticalTextAlignment { get; set; } = VerticalTextAlignment.Top;
    }
}
