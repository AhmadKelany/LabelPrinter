using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using LabelPrinter.Models;

namespace LabelPrinter.Helpers
{
    public static class LabelPrintingHelper
    {
        public static void PrintLabels(LabelDocument document, string printerName, int copies)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(printerName)) throw new ArgumentException("Printer name is required.", nameof(printerName));
            if (copies <= 0) throw new ArgumentOutOfRangeException(nameof(copies), "Label count must be greater than 0.");

            var validationErrors = LabelRenderer.ValidateDocument(document);
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
            }

            var snapshot = document.CreateSnapshot();
            var labelSize = LabelRenderer.GetLabelSize(snapshot);
            var printQueue = FindPrintQueue(printerName);
            var printTicket = CreatePrintTicket(printQueue, labelSize);
            var fixedDocument = CreateFixedDocument(snapshot, labelSize, copies);

            var xpsWriter = PrintQueue.CreateXpsDocumentWriter(printQueue);
            xpsWriter.Write(fixedDocument, printTicket);
        }

        private static PrintQueue FindPrintQueue(string printerName)
        {
            var localPrintServer = new LocalPrintServer();
            try
            {
                return localPrintServer.GetPrintQueue(printerName);
            }
            catch
            {
                foreach (var queue in localPrintServer.GetPrintQueues())
                {
                    if (string.Equals(queue.Name, printerName, StringComparison.OrdinalIgnoreCase))
                    {
                        return queue;
                    }
                }
            }

            throw new InvalidOperationException($"Printer '{printerName}' was not found.");
        }

        private static PrintTicket CreatePrintTicket(PrintQueue printQueue, Size labelSize)
        {
            var delta = new PrintTicket
            {
                PageMediaSize = new PageMediaSize(labelSize.Width, labelSize.Height)
            };

            try
            {
                var baseTicket = printQueue.UserPrintTicket ?? printQueue.DefaultPrintTicket;
                var result = printQueue.MergeAndValidatePrintTicket(baseTicket, delta);
                return result.ValidatedPrintTicket;
            }
            catch
            {
                return delta;
            }
        }

        private static FixedDocument CreateFixedDocument(LabelDocument snapshot, Size labelSize, int copies)
        {
            var document = new FixedDocument();
            document.DocumentPaginator.PageSize = labelSize;

            for (var i = 0; i < copies; i++)
            {
                var page = new FixedPage
                {
                    Width = labelSize.Width,
                    Height = labelSize.Height,
                    Background = Brushes.White
                };

                var labelElement = new LabelPrintElement(snapshot)
                {
                    Width = labelSize.Width,
                    Height = labelSize.Height
                };

                FixedPage.SetLeft(labelElement, 0);
                FixedPage.SetTop(labelElement, 0);
                page.Children.Add(labelElement);

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(page);
                document.Pages.Add(pageContent);
            }

            return document;
        }

        private sealed class LabelPrintElement : FrameworkElement
        {
            private readonly LabelDocument _document;

            public LabelPrintElement(LabelDocument document)
            {
                _document = document;
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                return LabelRenderer.GetLabelSize(_document);
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                LabelRenderer.DrawLabel(drawingContext, _document, new LabelRenderOptions
                {
                    DrawLabelBackground = true,
                    DrawLabelBorder = false,
                    ShowValidationErrors = false,
                    PixelsPerDip = 1.0
                });
            }
        }
    }
}
