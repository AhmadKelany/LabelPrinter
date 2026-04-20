using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Printing;
using LabelPrinter.Models;
using LabelPrinter.Helpers;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;
using System.Globalization;

namespace LabelPrinter;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<PrintableObject> _items = new ObservableCollection<PrintableObject>();
    private readonly DispatcherTimer _previewTimer;
    private const int PreviewDebounceMs = 300;

    public MainWindow()
    {
        InitializeComponent();

        ItemsListBox.ItemsSource = _items;

        // watch collection changes to attach/detach property changed handlers
        _items.CollectionChanged += Items_CollectionChanged;

        // preview debounce timer
        _previewTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(PreviewDebounceMs)
        };
        _previewTimer.Tick += PreviewTimer_Tick;

        // if collection changes initially, ensure preview updates (debounced)
        RestartPreviewTimer();

        // populate printers
        try
        {
            var server = new LocalPrintServer();
            foreach (var pq in server.GetPrintQueues())
            {
                PrintersCombo.Items.Add(pq.Name);
            }
            if (PrintersCombo.Items.Count > 0) PrintersCombo.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Printer enumeration failed: {ex}");
            // ignore
        }
    }

    private void AddImageBtn_Click(object sender, RoutedEventArgs e)
    {
        var img = new ImagePrintable { X = 10, Y = 10, Width = 100, Height = 50 };
        _items.Add(img);
    }

    private void AddTextBtn_Click(object sender, RoutedEventArgs e)
    {
        var txt = new TextPrintable { X = 10, Y = 10, Width = 100, Height = 30, Text = "Text" };
        _items.Add(txt);
    }

    private void AddBarcodeBtn_Click(object sender, RoutedEventArgs e)
    {
        var bc = new BarcodePrintable { X = 10, Y = 10, Width = 120, Height = 60, BarcodeText = "123456" };
        _items.Add(bc);
    }

    private void AddRectBtn_Click(object sender, RoutedEventArgs e)
    {
        var r = new RectanglePrintable { X = 5, Y = 5, Width = 150, Height = 80, StrokeThickness = 1 };
        _items.Add(r);
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var ni in e.NewItems)
            {
                if (ni is INotifyPropertyChanged inpc)
                {
                    inpc.PropertyChanged += Item_PropertyChanged;
                }
            }
        }

        if (e.OldItems != null)
        {
            foreach (var oi in e.OldItems)
            {
                if (oi is INotifyPropertyChanged inpc)
                {
                    inpc.PropertyChanged -= Item_PropertyChanged;
                }
            }
        }

        // Refresh preview on collection change (debounced)
        RestartPreviewTimer();
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // A property of an item changed; update preview (debounced)
        RestartPreviewTimer();
    }

    private void RefreshPreviewBtn_Click(object sender, RoutedEventArgs e)
    {
        // Immediate preview on manual refresh
        UpdatePreview();
    }

    private void RestartPreviewTimer()
    {
        if (_previewTimer?.IsEnabled == true)
        {
            _previewTimer.Stop();
        }
        // show pending indicator
        try
        {
            PreviewPendingPanel?.Visibility = Visibility.Visible;
        }
        catch (Exception ex) {
            System.Diagnostics.Trace.WriteLine($"Preview pending panel not initialized: {ex}");
        }
        _previewTimer?.Start();
    }

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        _previewTimer.Stop();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        // hide pending indicator
        try
        {
            PreviewPendingPanel.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"Unable to hide preview pending panel: {ex}"); }
        // Determine label dimensions from textboxes (cm -> device-independent units)
        double labelWidthUnits = 0;
        double labelHeightUnits = 0;
        if (double.TryParse(LabelWidthTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var wCm) && wCm > 0
            && double.TryParse(LabelHeightTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var hCm) && hCm > 0)
        {
            // 1 cm = 96 / 2.54 device-independent units
            const double unitsPerCm = 96.0 / 2.54;
            labelWidthUnits = wCm * unitsPerCm;
            labelHeightUnits = hCm * unitsPerCm;
        }

        // Render preview at fixed size; pass label size in device-independent units if available
        ImageSource img;
        if (labelWidthUnits > 0 && labelHeightUnits > 0)
            img = LabelPrintingHelper.RenderPreview(_items, 600, 800, labelWidthUnits, labelHeightUnits);
        else
            img = LabelPrintingHelper.RenderPreview(_items, 600, 800);
        PreviewImage.Source = img;
    }

    private void LabelSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RestartPreviewTimer();
    }

    private void PrintBtn_Click(object sender, RoutedEventArgs e)
    {
        if (PrintersCombo.SelectedItem == null) return;
        if (!int.TryParse(CountTextBox.Text, out int copies) || copies <= 0) copies = 1;
        var printerName = PrintersCombo.SelectedItem.ToString() ?? string.Empty;
        try
        {
            LabelPrintingHelper.PrintLabels(_items, printerName, copies);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Print failed: " + ex.Message);
        }
    }
}