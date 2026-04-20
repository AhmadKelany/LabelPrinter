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
        private double _cornerRadiusMm = 0.0;
        private double _strokeThicknessMm = 0.25;
        private Brush _stroke = Brushes.Black;
        private LineStyle _lineStyle = LineStyle.Continuous;
        private double[]? _dashPattern;

        public double CornerRadiusMm { get => _cornerRadiusMm; set => SetProperty(ref _cornerRadiusMm, value); }
        public double StrokeThicknessMm { get => _strokeThicknessMm; set => SetProperty(ref _strokeThicknessMm, value); }

        public Brush Stroke { get => _stroke; set => SetProperty(ref _stroke, value); }
        public LineStyle LineStyle { get => _lineStyle; set => SetProperty(ref _lineStyle, value); }
        public double[]? DashPattern { get => _dashPattern; set => SetProperty(ref _dashPattern, value); }

        public override PrintableObject Clone()
        {
            var clone = new RectanglePrintable
            {
                CornerRadiusMm = CornerRadiusMm,
                StrokeThicknessMm = StrokeThicknessMm,
                Stroke = Stroke,
                LineStyle = LineStyle,
                DashPattern = DashPattern?.ToArray()
            };
            CopyLayoutTo(clone);
            return clone;
        }

        protected override string GetValidationError(string propertyName)
        {
            return propertyName switch
            {
                nameof(StrokeThicknessMm) when !IsPositiveFinite(StrokeThicknessMm) => "Stroke thickness must be greater than 0 mm.",
                nameof(CornerRadiusMm) when !IsFinite(CornerRadiusMm) || CornerRadiusMm < 0 => "Corner radius must be 0 mm or greater.",
                _ => base.GetValidationError(propertyName)
            };
        }
    }
}
