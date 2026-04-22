using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using LabelPrinter.Helpers;
using LabelPrinter.Models;

namespace LabelPrinter.Controls
{
    public class LabelPreviewSurface : FrameworkElement
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(
                nameof(Document),
                typeof(LabelDocument),
                typeof(LabelPreviewSurface),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnDocumentChanged));

        public static readonly DependencyProperty HighlightedItemProperty =
            DependencyProperty.Register(
                nameof(HighlightedItem),
                typeof(PrintableObject),
                typeof(LabelPreviewSurface),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public LabelDocument? Document
        {
            get => (LabelDocument?)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        public PrintableObject? HighlightedItem
        {
            get => (PrintableObject?)GetValue(HighlightedItemProperty);
            set => SetValue(HighlightedItemProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return Document == null ? new Size(200, 120) : LabelRenderer.GetLabelSize(Document);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Document == null)
            {
                drawingContext.DrawRectangle(Brushes.White, new Pen(Brushes.Gray, 1.0), new Rect(RenderSize));
                return;
            }

            var dpi = VisualTreeHelper.GetDpi(this);
            LabelRenderer.DrawLabel(drawingContext, Document, new LabelRenderOptions
            {
                DrawLabelBackground = true,
                DrawLabelBorder = true,
                ShowValidationErrors = true,
                LabelCornerRadiusDip = 8.0,
                PixelsPerDip = dpi.PixelsPerDip,
                HighlightedItem = HighlightedItem
            });
        }

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is LabelDocument oldDocument)
            {
                oldDocument.PropertyChanged -= ((LabelPreviewSurface)d).Document_PropertyChanged;
            }

            if (e.NewValue is LabelDocument newDocument)
            {
                newDocument.PropertyChanged += ((LabelPreviewSurface)d).Document_PropertyChanged;
            }
        }

        private void Document_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            InvalidateMeasure();
            InvalidateVisual();
        }
    }
}
