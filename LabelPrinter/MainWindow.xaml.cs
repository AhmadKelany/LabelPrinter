using System.Collections.Specialized;
using System.ComponentModel;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LabelPrinter.Helpers;
using LabelPrinter.Models;

namespace LabelPrinter;

public partial class MainWindow : Window
{
    private readonly LabelDocument _document = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _document;
        ItemsListBox.ItemsSource = _document.Items;
        PreviewSurface.Document = _document;

        _document.PropertyChanged += Document_PropertyChanged;
        _document.Items.CollectionChanged += Items_CollectionChanged;
        AddHandler(Validation.ErrorEvent, new EventHandler<ValidationErrorEventArgs>(Window_ValidationError));

        LoadPrinters();
        UpdatePreview();
        UpdatePrintButtonState();
    }

    private void AddImageBtn_Click(object sender, RoutedEventArgs e)
    {
        _document.Items.Add(new ImagePrintable { XMm = 5, YMm = 5, WidthMm = 25, HeightMm = 15 });
    }

    private void AddTextBtn_Click(object sender, RoutedEventArgs e)
    {
        _document.Items.Add(new TextPrintable { XMm = 5, YMm = 5, WidthMm = 30, HeightMm = 8, Text = "Text" });
    }

    private void AddBarcodeBtn_Click(object sender, RoutedEventArgs e)
    {
        _document.Items.Add(new BarcodePrintable { XMm = 5, YMm = 12, WidthMm = 38, HeightMm = 14, BarcodeText = "123456" });
    }

    private void AddRectBtn_Click(object sender, RoutedEventArgs e)
    {
        _document.Items.Add(new RectanglePrintable { XMm = 3, YMm = 3, WidthMm = 35, HeightMm = 18, StrokeThicknessMm = 0.25 });
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PrintableObject item })
        {
            _document.Items.Remove(item);
        }
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems.OfType<INotifyPropertyChanged>())
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
        }

        UpdatePreview();
        UpdatePrintButtonState();
    }

    private void Document_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdatePreview();
        UpdatePrintButtonState();
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdatePreview();
        UpdatePrintButtonState();
    }

    private void CountTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateCountValidationState();
        UpdatePrintButtonState();
    }

    private void PrintersCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePrintButtonState();
    }

    private void Window_ValidationError(object? sender, ValidationErrorEventArgs e)
    {
        UpdatePrintButtonState();
    }

    private void PrintBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!TryValidateBeforePrint(out var copies, out var printerName, out var message))
        {
            MessageBox.Show(this, message, "Cannot print", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            LabelPrintingHelper.PrintLabels(_document, printerName, copies);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Print failed: " + ex.Message, "Print failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadPrinters()
    {
        try
        {
            var server = new LocalPrintServer();
            foreach (var queue in server.GetPrintQueues())
            {
                PrintersCombo.Items.Add(queue.Name);
            }

            if (PrintersCombo.Items.Count > 0)
            {
                PrintersCombo.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Printer enumeration failed: {ex}");
        }
    }

    private void UpdatePreview()
    {
        PreviewSurface?.InvalidateMeasure();
        PreviewSurface?.InvalidateVisual();
    }

    private void UpdatePrintButtonState()
    {
        if (PrintBtn == null) return;
        PrintBtn.IsEnabled =
            PrintersCombo?.SelectedItem != null &&
            TryGetCopies(out _) &&
            !HasValidationError(this) &&
            LabelRenderer.ValidateDocument(_document).Count == 0;
    }

    private void UpdateCountValidationState()
    {
        if (CountTextBox == null) return;

        if (TryGetCopies(out _))
        {
            CountTextBox.ClearValue(BorderBrushProperty);
            CountTextBox.ClearValue(ToolTipProperty);
            return;
        }

        CountTextBox.BorderBrush = Brushes.Red;
        CountTextBox.ToolTip = "Label count must be a whole number greater than 0.";
    }

    private bool TryValidateBeforePrint(out int copies, out string printerName, out string message)
    {
        copies = 0;
        printerName = PrintersCombo.SelectedItem?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(printerName))
        {
            message = "Select a printer first.";
            return false;
        }

        if (!TryGetCopies(out copies))
        {
            message = "Label count must be a whole number greater than 0.";
            return false;
        }

        if (HasValidationError(this))
        {
            message = "Fix the highlighted fields before printing.";
            return false;
        }

        var validationErrors = LabelRenderer.ValidateDocument(_document);
        if (validationErrors.Count > 0)
        {
            message = string.Join(Environment.NewLine, validationErrors);
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool TryGetCopies(out int copies)
    {
        return int.TryParse(CountTextBox?.Text, out copies) && copies > 0;
    }

    private static bool HasValidationError(DependencyObject root)
    {
        if (Validation.GetHasError(root)) return true;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (HasValidationError(VisualTreeHelper.GetChild(root, i))) return true;
        }

        return false;
    }
}
