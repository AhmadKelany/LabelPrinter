using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LabelPrinter.Models;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace LabelPrinter.Helpers
{
    public static class LabelPrintingHelper
    {
        /// <summary>
        /// Print a number of labels using the provided printable objects definition.
        /// Each PrintableObject should have X, Y, Width and Height set in device-independent units (1/96 inch).
        /// </summary>
        public static void PrintLabels(IEnumerable<PrintableObject> printableObjects, string printerName, int copies)
        {
            if (printableObjects == null) throw new ArgumentNullException(nameof(printableObjects));
            if (string.IsNullOrWhiteSpace(printerName)) throw new ArgumentException("printerName");
            if (copies <= 0) throw new ArgumentOutOfRangeException(nameof(copies));

            LocalPrintServer localPrintServer = new LocalPrintServer();
            PrintQueue? printQueue = null;
            try
            {
                // Try to find the named print queue
                printQueue = localPrintServer.GetPrintQueue(printerName);
            }


            catch
            {
                // fallback: try to find by enumerating
                foreach (var pq in localPrintServer.GetPrintQueues())
                {
                    if (string.Equals(pq.Name, printerName, StringComparison.OrdinalIgnoreCase))
                    {
                        printQueue = pq;
                        break;
                    }
                }
            }

            if (printQueue == null)
                throw new InvalidOperationException($"Printer '{printerName}' not found.");

            var xpsWriter = PrintQueue.CreateXpsDocumentWriter(printQueue);

            for (int i = 0; i < copies; i++)
            {
                var visual = RenderLabelVisual(printableObjects);
                // Write visual to the specified printer
                xpsWriter.Write(visual);
            }
        }

        private static DrawingVisual RenderLabelVisual(IEnumerable<PrintableObject> printableObjects)
        {
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                foreach (var obj in printableObjects)
                {
                    dc.PushTransform(new TranslateTransform(obj.X, obj.Y));
                    if (obj.Rotation != 0)
                    {
                        // rotate around top-left + half width/height
                        dc.PushTransform(new RotateTransform(obj.Rotation, obj.Width / 2.0, obj.Height / 2.0));
                    }

                    Rect dest = new Rect(0, 0, Math.Max(0.0, obj.Width), Math.Max(0.0, obj.Height));

                    switch (obj)
                    {
                        case ImagePrintable img:
                            if (!string.IsNullOrWhiteSpace(img.ImagePath) && File.Exists(img.ImagePath))
                            {
                                try
                                {
                                    var bmp = new BitmapImage();
                                    bmp.BeginInit();
                                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                                    bmp.UriSource = new Uri(Path.GetFullPath(img.ImagePath));
                                    bmp.EndInit();
                                    bmp.Freeze();

                                    // Optionally preserve aspect ratio
                                    if (img.MaintainAspectRatio)
                                    {
                                        double w = bmp.PixelWidth / bmp.DpiX * 96.0;
                                        double h = bmp.PixelHeight / bmp.DpiY * 96.0;
                                        double scale = Math.Min(dest.Width / w, dest.Height / h);
                                        double drawW = w * scale;
                                        double drawH = h * scale;
                                        double offsetX = (dest.Width - drawW) / 2.0;
                                        double offsetY = (dest.Height - drawH) / 2.0;
                                        dc.DrawImage(bmp, new Rect(offsetX, offsetY, drawW, drawH));
                                    }
                                    else
                                    {
                                        dc.DrawImage(bmp, dest);
                                    }
                                }
                                catch
                                {
                                    // Ignore image loading errors for now
                                }
                            }
                            break;

                        case TextPrintable txt:
                            {
                                if (!string.IsNullOrEmpty(txt.Text))
                                {
                                    var typeface = new Typeface(new System.Windows.Media.FontFamily(txt.FontFamily), txt.FontStyle, txt.FontWeight, FontStretches.Normal);
                                    // pixelsPerDip: use 1.0 as a reasonable default
                                    var formatted = new FormattedText(
                                        txt.Text,
                                        CultureInfo.CurrentUICulture,
                                        FlowDirection.LeftToRight,
                                        typeface,
                                        txt.FontSize,
                                        txt.Foreground,
                                        1.0);

                                    // constrain width/height
                                    formatted.MaxTextWidth = Math.Max(0.0, txt.Width);
                                    formatted.MaxTextHeight = Math.Max(0.0, txt.Height);

                                    // apply horizontal alignment
                                    formatted.TextAlignment = txt.HorizontalTextAlignment;

                                    double drawX = 0;
                                    switch (txt.HorizontalTextAlignment)
                                    {
                                        case TextAlignment.Center:
                                            drawX = (dest.Width - formatted.Width) / 2.0;
                                            break;
                                        case TextAlignment.Right:
                                            drawX = dest.Width - formatted.Width;
                                            break;
                                        case TextAlignment.Justify:
                                        case TextAlignment.Left:
                                        default:
                                            drawX = 0;
                                            break;
                                    }

                                    // apply vertical alignment
                                    double drawY = 0;
                                    switch (txt.VerticalTextAlignment)
                                    {
                                        case VerticalTextAlignment.Center:
                                            drawY = (dest.Height - formatted.Height) / 2.0;
                                            break;
                                        case VerticalTextAlignment.Bottom:
                                            drawY = dest.Height - formatted.Height;
                                            break;
                                        case VerticalTextAlignment.Top:
                                        default:
                                            drawY = 0;
                                            break;
                                    }

                                    drawX = Math.Max(0.0, drawX);
                                    drawY = Math.Max(0.0, drawY);

                                    dc.DrawText(formatted, new Point(drawX, drawY));
                                }
                            }
                            break;

                        case BarcodePrintable bc:
                            {
                                if (!string.IsNullOrEmpty(bc.BarcodeText))
                                {
                                    var format = bc.BarcodeType == BarcodeImageType.QR ? BarcodeFormat.QR_CODE : BarcodeFormat.CODE_128;

                                    var writer = new BarcodeWriterPixelData
                                    {
                                        Format = format,
                                        Options = new EncodingOptions
                                        {
                                            Width = Math.Max(1, (int)Math.Round(bc.Width)),
                                            Height = Math.Max(1, (int)Math.Round(bc.Height)),
                                            Margin = 0
                                        }
                                    };

                                    try
                                    {
                                        var pixelData = writer.Write(bc.BarcodeText);

                                        // Create BitmapSource from pixel data. ZXing returns RGB24-like bytes in PixelData by default
                                        PixelFormat pixFmt = PixelFormats.Gray8;
                                        int stride = pixelData.Width * (pixFmt.BitsPerPixel / 8);

                                        BitmapSource bitmap = BitmapSource.Create(pixelData.Width, pixelData.Height, 96, 96, pixFmt, null, pixelData.Pixels, stride);
                                        bitmap.Freeze();

                                        dc.DrawImage(bitmap, dest);

                                        if (bc.ShowBarcodeText)
                                        {
                                            var tf = new Typeface("Segoe UI");
                                            var ft = new FormattedText(bc.BarcodeText, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, tf, 8, Brushes.Black, 1.0)
                                            {
                                                MaxTextWidth = dest.Width
                                            };

                                            dc.DrawText(ft, new Point(0, dest.Height + 2));
                                        }
                                    }
                                    catch
                                    {
                                        // ignore barcode generation errors
                                    }
                                }
                            }
                            break;

                        case RectanglePrintable rect:
                            {
                                // No fill: rectangles are stroke-only to avoid interfering with other objects.

                                // Prepare pen for stroke
                                var pen = new Pen(rect.Stroke ?? Brushes.Black, Math.Max(0.0, rect.StrokeThickness));
                                // Apply dash/dot/solid or custom dash pattern
                                if (rect.DashPattern != null && rect.DashPattern.Length > 0)
                                {
                                    pen.DashStyle = new DashStyle(rect.DashPattern, 0.0);
                                }
                                else
                                {
                                    switch (rect.LineStyle)
                                    {
                                        case LineStyle.Dash:
                                            pen.DashStyle = DashStyles.Dash;
                                            break;
                                        case LineStyle.Dot:
                                            pen.DashStyle = DashStyles.Dot;
                                            break;
                                        case LineStyle.Continuous:
                                        default:
                                            pen.DashStyle = DashStyles.Solid;
                                            break;
                                    }
                                }
                                pen.Freeze();

                                if (rect.CornerRadius > 0)
                                {
                                    dc.DrawRoundedRectangle(null, pen, dest, rect.CornerRadius, rect.CornerRadius);
                                }
                                else
                                {
                                    dc.DrawRectangle(null, pen, dest);
                                }
                            }
                            break;

                        default:
                            break;
                    }

                    if (obj.Rotation != 0)
                    {
                        dc.Pop(); // pop rotate
                    }
                    dc.Pop(); // pop translate
                }
            }

            return dv;
        }

        /// <summary>
        /// Renders the printable objects into an ImageSource for preview purposes.
        /// pixelWidth/pixelHeight are the output bitmap size in pixels.
        /// labelWidth and labelHeight are the physical label size in device-independent units (1/96 inch).
        /// If labelWidth/labelHeight are not provided (<=0) the content is rendered to fill the bitmap.
        /// </summary>
        public static ImageSource RenderPreview(IEnumerable<PrintableObject> printableObjects, int pixelWidth, int pixelHeight, double labelWidth = 0, double labelHeight = 0)
        {
            if (pixelWidth <= 0) pixelWidth = 400;
            if (pixelHeight <= 0) pixelHeight = 300;

            var contentVisual = RenderLabelVisual(printableObjects);

            var container = new DrawingVisual();
            using (var dc = container.RenderOpen())
            {
                // If label dimensions provided, draw a white label rectangle scaled to fit the bitmap.
                if (labelWidth > 0 && labelHeight > 0)
                {
                    double scaleX = (double)pixelWidth / labelWidth;
                    double scaleY = (double)pixelHeight / labelHeight;
                    double scale = Math.Min(scaleX, scaleY);

                    double drawW = labelWidth * scale;
                    double drawH = labelHeight * scale;

                    double offsetX = Math.Floor(((double)pixelWidth - drawW) / 2.0);
                    double offsetY = Math.Floor(((double)pixelHeight - drawH) / 2.0);

                    // Draw background (transparent) then white label area with border
                    var labelRect = new Rect(offsetX, offsetY, drawW, drawH);
                    dc.DrawRectangle(Brushes.White, new Pen(Brushes.Black, 1.0), labelRect);

                    // Draw the contentVisual into the label area using a VisualBrush scaled appropriately
                    var vb = new VisualBrush(contentVisual)
                    {
                        Stretch = Stretch.None,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top,
                        Transform = new ScaleTransform(scale, scale)
                    };

                    dc.DrawRectangle(vb, null, labelRect);
                }
                else
                {
                    // No label size: render contentVisual to fill bitmap
                    var vb = new VisualBrush(contentVisual) { Stretch = Stretch.Uniform };
                    dc.DrawRectangle(vb, null, new Rect(0, 0, pixelWidth, pixelHeight));
                }
            }

            var bmp = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(container);
            bmp.Freeze();
            return bmp;
        }
    }
}
