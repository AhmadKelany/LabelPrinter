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
        private System.Windows.TextAlignment _horizontalTextAlignment = System.Windows.TextAlignment.Left;
        private VerticalTextAlignment _verticalTextAlignment = VerticalTextAlignment.Top;

        public string Text { get => _text; set => SetProperty(ref _text, value); }
        public string FontFamily { get => _fontFamily; set => SetProperty(ref _fontFamily, value); }
        public double FontSize { get => _fontSize; set => SetProperty(ref _fontSize, value); }
        public FontStyle FontStyle { get => _fontStyle; set => SetProperty(ref _fontStyle, value); }
        public FontWeight FontWeight { get => _fontWeight; set => SetProperty(ref _fontWeight, value); }
        public Brush Foreground { get => _foreground; set => SetProperty(ref _foreground, value); }
        public System.Windows.TextAlignment HorizontalTextAlignment { get => _horizontalTextAlignment; set => SetProperty(ref _horizontalTextAlignment, value); }
        public VerticalTextAlignment VerticalTextAlignment { get => _verticalTextAlignment; set => SetProperty(ref _verticalTextAlignment, value); }

        public override PrintableObject Clone()
        {
            var clone = new TextPrintable
            {
                Text = Text,
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontStyle = FontStyle,
                FontWeight = FontWeight,
                Foreground = Foreground,
                HorizontalTextAlignment = HorizontalTextAlignment,
                VerticalTextAlignment = VerticalTextAlignment
            };
            CopyLayoutTo(clone);
            return clone;
        }

        protected override string GetValidationError(string propertyName)
        {
            return propertyName switch
            {
                nameof(FontSize) when !IsPositiveFinite(FontSize) => "Font size must be greater than 0.",
                _ => base.GetValidationError(propertyName)
            };
        }
    }
}
