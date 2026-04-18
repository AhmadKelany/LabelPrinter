using System;

namespace LabelPrinter.Models
{
    public class ImagePrintable : PrintableObject
    {
        private string _imagePath = string.Empty;
        private bool _maintainAspectRatio = true;

        // Path to the image file to print
        public string ImagePath { get => _imagePath; set => SetProperty(ref _imagePath, value); }

        // Optionally allow stretching mode or maintain aspect ratio in rendering later
        public bool MaintainAspectRatio { get => _maintainAspectRatio; set => SetProperty(ref _maintainAspectRatio, value); }
    }
}
