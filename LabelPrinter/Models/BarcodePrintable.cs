using System;

namespace LabelPrinter.Models
{
    public enum BarcodeImageType
    {
        OneD,
        QR
    }

    public class BarcodePrintable : PrintableObject
    {
        private string _barcodeText = string.Empty;
        private BarcodeImageType _barcodeType = BarcodeImageType.OneD;
        private bool _showBarcodeText = false;

        // The data encoded in the barcode
        public string BarcodeText { get => _barcodeText; set => SetProperty(ref _barcodeText, value); }

        // Type of barcode image to generate
        public BarcodeImageType BarcodeType { get => _barcodeType; set => SetProperty(ref _barcodeType, value); }

        // Show the human-readable text under the barcode image
        public bool ShowBarcodeText { get => _showBarcodeText; set => SetProperty(ref _showBarcodeText, value); }
    }
}
