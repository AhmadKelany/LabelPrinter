using System.Windows.Media;

namespace LabelPrinter.Models
{
    public enum LineStyle
    {
        Continuous,
        Dash,
        Dot
    }

    public class RectanglePrintable : PrintableObject
    {
        private double _cornerRadius = 0.0;
        private double _strokeThickness = 1.0;
        private Brush _stroke = Brushes.Black;
        private LineStyle _lineStyle = LineStyle.Continuous;
        private double[]? _dashPattern;

        // Corner radius in device-independent units
        public double CornerRadius { get => _cornerRadius; set => SetProperty(ref _cornerRadius, value); }

        // Stroke thickness
        public double StrokeThickness { get => _strokeThickness; set => SetProperty(ref _strokeThickness, value); }

        // Stroke brush
        public Brush Stroke { get => _stroke; set => SetProperty(ref _stroke, value); }

        // Line style for the stroke
        public LineStyle LineStyle { get => _lineStyle; set => SetProperty(ref _lineStyle, value); }

        // Optional custom dash pattern. If set (non-empty), it overrides LineStyle.
        // Values represent lengths of dashes and gaps in device-independent units.
        public double[]? DashPattern { get => _dashPattern; set => SetProperty(ref _dashPattern, value); }
    }
}
