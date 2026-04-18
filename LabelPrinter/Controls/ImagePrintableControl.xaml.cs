using System.Windows.Controls;
using Microsoft.Win32;
using LabelPrinter.Models;


namespace LabelPrinter.Controls
{
    public partial class ImagePrintableControl : UserControl
    {
        public ImagePrintableControl()
        {
            InitializeComponent();
        }

        private void BrowseBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Title = "Select image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                // DataContext should be ImagePrintable when used inside the DataTemplate
                if (this.DataContext is ImagePrintable img)
                {
                    img.ImagePath = dlg.FileName;
                }
                else
                {
                    // Fallback: set the textbox value directly
                    PathTextBox.Text = dlg.FileName;
                }
            }
        }
    }
}
