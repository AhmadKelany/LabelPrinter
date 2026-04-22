using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LabelPrinter.Models;
using ZXing;
using ZXing.Common;

namespace LabelPrinter.Helpers
{
    public static class LabelRenderer
    {
        public const double DipsPerMillimeter = 96.0 / 25.4;

        public static double MmToDip(double valueMm) => valueMm * DipsPerMillimeter;

        public static Size GetLabelSize(LabelDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return new Size(
                Math.Max(1.0, MmToDip(document.WidthMm)),
                Math.Max(1.0, MmToDip(document.HeightMm)));
        }

        public static IReadOnlyList<string> ValidateDocument(LabelDocument document)
        {
            var errors = new List<string>();
            if (document == null)
            {
                errors.Add("The label document is missing.");
                return errors;
            }

            if (!IsPositiveFinite(document.WidthMm)) errors.Add("Label width must be greater than 0 mm.");
            if (!IsPositiveFinite(document.HeightMm)) errors.Add("Label height must be greater than 0 mm.");

            for (var i = 0; i < document.Items.Count; i++)
            {
                var item = document.Items[i];
                var name = $"{item.GetType().Name} #{i + 1}";

                if (!IsFinite(item.XMm) || item.XMm < 0) errors.Add($"{name}: X must be 0 mm or greater.");
                if (!IsFinite(item.YMm) || item.YMm < 0) errors.Add($"{name}: Y must be 0 mm or greater.");
                if (!IsPositiveFinite(item.WidthMm)) errors.Add($"{name}: width must be greater than 0 mm.");
                if (!IsPositiveFinite(item.HeightMm)) errors.Add($"{name}: height must be greater than 0 mm.");
                if (!IsFinite(item.RotationDegrees)) errors.Add($"{name}: rotation must be a valid number.");

                switch (item)
                {
                    case TextPrintable text:
                        if (!IsPositiveFinite(text.FontSize)) errors.Add($"{name}: font size must be greater than 0.");
                        break;
                    case BarcodePrintable barcode:
                        if (string.IsNullOrWhiteSpace(barcode.BarcodeText)) errors.Add($"{name}: barcode data is required.");
                        break;
                    case ImagePrintable image:
                        if (string.IsNullOrWhiteSpace(image.ImagePath)) errors.Add($"{name}: image path is required.");
                        else if (!File.Exists(image.ImagePath)) errors.Add($"{name}: image file was not found.");
                        break;
                    case RectanglePrintable rectangle:
                        if (!IsPositiveFinite(rectangle.StrokeThicknessMm)) errors.Add($"{name}: stroke thickness must be greater than 0 mm.");
                        if (!IsFinite(rectangle.CornerRadiusMm) || rectangle.CornerRadiusMm < 0) errors.Add($"{name}: corner radius must be 0 mm or greater.");
                        break;
                }
            }

            return errors;
        }

        public static void DrawLabel(DrawingContext dc, LabelDocument document, LabelRenderOptions? options = null)
        {
            if (dc == null) throw new ArgumentNullException(nameof(dc));
            if (document == null) throw new ArgumentNullException(nameof(document));

            options ??= new LabelRenderOptions();
            var labelSize = GetLabelSize(document);
            var labelRect = new Rect(0, 0, labelSize.Width, labelSize.Height);
            var radius = Math.Max(0.0, options.LabelCornerRadiusDip);

            if (options.DrawLabelBackground)
            {
                dc.DrawRoundedRectangle(Brushes.White, null, labelRect, radius, radius);
            }

            dc.PushClip(new RectangleGeometry(labelRect));
            foreach (var item in document.Items)
            {
                DrawObject(dc, item, options);
            }
            dc.Pop();

            if (options.DrawLabelBorder)
            {
                var borderPen = new Pen(Brushes.Gray, 1.0);
                borderPen.Freeze();
                dc.DrawRoundedRectangle(null, borderPen, labelRect, radius, radius);
            }
        }

        private static void DrawObject(DrawingContext dc, PrintableObject item, LabelRenderOptions options)
        {
            var rect = new Rect(
                MmToDip(item.XMm),
                MmToDip(item.YMm),
                MmToDip(item.WidthMm),
                MmToDip(item.HeightMm));

            if (!IsPositiveFinite(item.WidthMm) || !IsPositiveFinite(item.HeightMm))
            {
                if (options.ShowValidationErrors)
                {
                    DrawValidationMessage(dc, FallbackRect(item), "Invalid size", options);
                }
                return;
            }

            var pushedRotation = false;
            if (IsFinite(item.RotationDegrees) && Math.Abs(item.RotationDegrees) > 0.001)
            {
                dc.PushTransform(new RotateTransform(item.RotationDegrees, rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0));
                pushedRotation = true;
            }

            if (ReferenceEquals(options.HighlightedItem, item) && options.HighlightBrush != null)
            {
                dc.DrawRectangle(options.HighlightBrush, null, rect);
            }

            dc.PushClip(new RectangleGeometry(rect));
            try
            {
                switch (item)
                {
                    case ImagePrintable image:
                        DrawImage(dc, image, rect, options);
                        break;
                    case TextPrintable text:
                        DrawText(dc, text, rect, options);
                        break;
                    case BarcodePrintable barcode:
                        DrawBarcode(dc, barcode, rect, options);
                        break;
                    case RectanglePrintable rectangle:
                        DrawRectangle(dc, rectangle, rect);
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Unable to draw {item.GetType().Name}: {ex}");
                if (options.ShowValidationErrors)
                {
                    DrawValidationMessage(dc, rect, "Render error", options);
                }
            }
            finally
            {
                dc.Pop();
                if (pushedRotation) dc.Pop();
            }
        }

        private static void DrawImage(DrawingContext dc, ImagePrintable image, Rect rect, LabelRenderOptions options)
        {
            if (string.IsNullOrWhiteSpace(image.ImagePath) || !File.Exists(image.ImagePath))
            {
                if (options.ShowValidationErrors) DrawValidationMessage(dc, rect, "Missing image", options);
                return;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(Path.GetFullPath(image.ImagePath), UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            if (!image.MaintainAspectRatio)
            {
                dc.DrawImage(bitmap, rect);
                return;
            }

            var dpiX = bitmap.DpiX > 0 ? bitmap.DpiX : 96.0;
            var dpiY = bitmap.DpiY > 0 ? bitmap.DpiY : 96.0;
            var sourceWidth = bitmap.PixelWidth / dpiX * 96.0;
            var sourceHeight = bitmap.PixelHeight / dpiY * 96.0;

            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                dc.DrawImage(bitmap, rect);
                return;
            }

            var scale = Math.Min(rect.Width / sourceWidth, rect.Height / sourceHeight);
            var drawWidth = sourceWidth * scale;
            var drawHeight = sourceHeight * scale;
            var drawRect = new Rect(
                rect.X + (rect.Width - drawWidth) / 2.0,
                rect.Y + (rect.Height - drawHeight) / 2.0,
                drawWidth,
                drawHeight);

            dc.DrawImage(bitmap, drawRect);
        }

        private static void DrawText(DrawingContext dc, TextPrintable text, Rect rect, LabelRenderOptions options)
        {
            if (string.IsNullOrEmpty(text.Text)) return;

            var typeface = new Typeface(
                new FontFamily(string.IsNullOrWhiteSpace(text.FontFamily) ? "Segoe UI" : text.FontFamily),
                text.FontStyle,
                text.FontWeight,
                FontStretches.Normal);

            var formatted = new FormattedText(
                text.Text,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                Math.Max(1.0, text.FontSize),
                text.Foreground ?? Brushes.Black,
                Math.Max(0.1, options.PixelsPerDip))
            {
                MaxTextWidth = Math.Max(1.0, rect.Width),
                MaxTextHeight = Math.Max(1.0, rect.Height),
                TextAlignment = text.HorizontalTextAlignment
            };

            var drawY = text.VerticalTextAlignment switch
            {
                VerticalTextAlignment.Center => rect.Y + Math.Max(0.0, (rect.Height - formatted.Height) / 2.0),
                VerticalTextAlignment.Bottom => rect.Y + Math.Max(0.0, rect.Height - formatted.Height),
                _ => rect.Y
            };

            dc.DrawText(formatted, new Point(rect.X, drawY));
        }

        private static void DrawBarcode(DrawingContext dc, BarcodePrintable barcode, Rect rect, LabelRenderOptions options)
        {
            if (string.IsNullOrWhiteSpace(barcode.BarcodeText))
            {
                if (options.ShowValidationErrors) DrawValidationMessage(dc, rect, "Missing data", options);
                return;
            }

            var textHeight = barcode.ShowBarcodeText ? Math.Min(MmToDip(5.0), rect.Height * 0.28) : 0.0;
            var spacing = barcode.ShowBarcodeText && textHeight > 0 ? Math.Min(MmToDip(1.0), rect.Height * 0.05) : 0.0;
            var barcodeHeight = Math.Max(1.0, rect.Height - textHeight - spacing);
            var barcodeRect = new Rect(rect.X, rect.Y, rect.Width, barcodeHeight);

            if (barcode.BarcodeType == BarcodeImageType.QR)
            {
                var side = Math.Min(barcodeRect.Width, barcodeRect.Height);
                barcodeRect = new Rect(
                    barcodeRect.X + (barcodeRect.Width - side) / 2.0,
                    barcodeRect.Y + (barcodeRect.Height - side) / 2.0,
                    side,
                    side);
            }

            dc.DrawRectangle(Brushes.White, null, barcodeRect);
            var matrix = CreateBarcodeMatrix(barcode, barcodeRect, options);

            if (barcode.BarcodeType == BarcodeImageType.OneD)
            {
                DrawOneDimensionalMatrix(dc, matrix, barcodeRect);
            }
            else
            {
                DrawTwoDimensionalMatrix(dc, matrix, barcodeRect);
            }

            if (barcode.ShowBarcodeText && textHeight > 0)
            {
                var textRect = new Rect(rect.X, rect.Y + barcodeHeight + spacing, rect.Width, textHeight);
                var formatted = new FormattedText(
                    barcode.BarcodeText,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    Math.Max(6.0, textHeight * 0.65),
                    Brushes.Black,
                    Math.Max(0.1, options.PixelsPerDip))
                {
                    MaxTextWidth = Math.Max(1.0, textRect.Width),
                    MaxTextHeight = Math.Max(1.0, textRect.Height),
                    TextAlignment = TextAlignment.Center
                };

                dc.DrawText(formatted, new Point(textRect.X, textRect.Y));
            }
        }

        private static BitMatrix CreateBarcodeMatrix(BarcodePrintable barcode, Rect barcodeRect, LabelRenderOptions options)
        {
            var format = barcode.BarcodeType == BarcodeImageType.QR ? BarcodeFormat.QR_CODE : BarcodeFormat.CODE_128;
            var width = Math.Max(1, (int)Math.Round(barcodeRect.Width));
            var height = Math.Max(1, (int)Math.Round(barcodeRect.Height));
            var hints = new Dictionary<EncodeHintType, object>
            {
                [EncodeHintType.MARGIN] = Math.Max(0, options.BarcodeQuietZoneModules)
            };

            return new MultiFormatWriter().encode(barcode.BarcodeText, format, width, height, hints);
        }

        private static void DrawOneDimensionalMatrix(DrawingContext dc, BitMatrix matrix, Rect rect)
        {
            var cellWidth = rect.Width / matrix.Width;
            for (var x = 0; x < matrix.Width; x++)
            {
                var black = false;
                for (var y = 0; y < matrix.Height && !black; y++)
                {
                    black = matrix[x, y];
                }

                if (!black) continue;
                dc.DrawRectangle(Brushes.Black, null, new Rect(
                    rect.X + x * cellWidth,
                    rect.Y,
                    Math.Ceiling(cellWidth) + 0.25,
                    rect.Height));
            }
        }

        private static void DrawTwoDimensionalMatrix(DrawingContext dc, BitMatrix matrix, Rect rect)
        {
            var cellWidth = rect.Width / matrix.Width;
            var cellHeight = rect.Height / matrix.Height;

            for (var y = 0; y < matrix.Height; y++)
            {
                for (var x = 0; x < matrix.Width; x++)
                {
                    if (!matrix[x, y]) continue;
                    dc.DrawRectangle(Brushes.Black, null, new Rect(
                        rect.X + x * cellWidth,
                        rect.Y + y * cellHeight,
                        cellWidth + 0.25,
                        cellHeight + 0.25));
                }
            }
        }

        private static void DrawRectangle(DrawingContext dc, RectanglePrintable rectangle, Rect rect)
        {
            var strokeThickness = Math.Max(0.1, MmToDip(rectangle.StrokeThicknessMm));
            var pen = new Pen(rectangle.Stroke ?? Brushes.Black, strokeThickness);

            if (rectangle.DashPattern is { Length: > 0 })
            {
                pen.DashStyle = new DashStyle(rectangle.DashPattern.Select(MmToDip).ToArray(), 0.0);
            }
            else
            {
                pen.DashStyle = rectangle.LineStyle switch
                {
                    LineStyle.Dash => DashStyles.Dash,
                    LineStyle.Dot => DashStyles.Dot,
                    _ => DashStyles.Solid
                };
            }

            pen.Freeze();
            var inset = strokeThickness / 2.0;
            var strokeRect = new Rect(
                rect.X + inset,
                rect.Y + inset,
                Math.Max(0.0, rect.Width - strokeThickness),
                Math.Max(0.0, rect.Height - strokeThickness));

            var radius = Math.Max(0.0, MmToDip(rectangle.CornerRadiusMm));
            if (radius > 0)
            {
                dc.DrawRoundedRectangle(null, pen, strokeRect, radius, radius);
            }
            else
            {
                dc.DrawRectangle(null, pen, strokeRect);
            }
        }

        private static Rect FallbackRect(PrintableObject item)
        {
            return new Rect(
                Math.Max(0.0, MmToDip(IsFinite(item.XMm) ? item.XMm : 0.0)),
                Math.Max(0.0, MmToDip(IsFinite(item.YMm) ? item.YMm : 0.0)),
                MmToDip(12.0),
                MmToDip(7.0));
        }

        private static void DrawValidationMessage(DrawingContext dc, Rect rect, string message, LabelRenderOptions options)
        {
            var pen = new Pen(Brushes.Red, 1.0) { DashStyle = DashStyles.Dash };
            pen.Freeze();
            dc.DrawRectangle(null, pen, rect);

            if (rect.Width < 12 || rect.Height < 8) return;

            var formatted = new FormattedText(
                message,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                8.0,
                Brushes.Red,
                Math.Max(0.1, options.PixelsPerDip))
            {
                MaxTextWidth = Math.Max(1.0, rect.Width),
                MaxTextHeight = Math.Max(1.0, rect.Height)
            };

            dc.DrawText(formatted, new Point(rect.X + 2, rect.Y + 2));
        }

        private static bool IsPositiveFinite(double value) => IsFinite(value) && value > 0.0;
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
