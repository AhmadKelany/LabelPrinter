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

        LoadSavedDesigns();
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
            if (ReferenceEquals(PreviewSurface.HighlightedItem, item))
            {
                PreviewSurface.HighlightedItem = null;
            }

            _document.Items.Remove(item);
        }
    }

    private void PrintableControl_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PrintableObject item })
        {
            return;
        }

        if (e.NewValue is true)
        {
            PreviewSurface.HighlightedItem = item;
        }
        else if (ReferenceEquals(PreviewSurface.HighlightedItem, item))
        {
            PreviewSurface.HighlightedItem = null;
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

    private void SaveDesignBtn_Click(object sender, RoutedEventArgs e)
    {
        string designName;
        try
        {
            designName = LabelDesignStore.NormalizeDesignName(SavedDesignsCombo.Text);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Cannot save design", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var path = LabelDesignStore.GetDesignPath(designName);
            if (System.IO.File.Exists(path))
            {
                var result = MessageBox.Show(
                    this,
                    $"Overwrite the saved design '{designName}'?",
                    "Overwrite design",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var savedName = LabelDesignStore.Save(_document, designName);
            LoadSavedDesigns(savedName);
            MessageBox.Show(this, $"Design saved as '{savedName}'.", "Design saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Save failed: " + ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadDesignBtn_Click(object sender, RoutedEventArgs e)
    {
        string designName;
        try
        {
            designName = LabelDesignStore.NormalizeDesignName(SavedDesignsCombo.Text);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Cannot load design", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var loadedDocument = LabelDesignStore.Load(designName);
            ApplyLoadedDocument(loadedDocument);
            LoadSavedDesigns(designName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Load failed: " + ex.Message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

    private void LoadSavedDesigns(string? selectedDesignName = null)
    {
        try
        {
            var designNames = LabelDesignStore.GetSavedDesignNames();
            var designName = selectedDesignName ?? SavedDesignsCombo.Text;

            SavedDesignsCombo.ItemsSource = designNames;
            SavedDesignsCombo.Text = !string.IsNullOrWhiteSpace(designName)
                ? designName
                : designNames.FirstOrDefault() ?? "Untitled";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Design enumeration failed: {ex}");
            SavedDesignsCombo.Text = "Untitled";
        }
    }

    private void ApplyLoadedDocument(LabelDocument loadedDocument)
    {
        PreviewSurface.HighlightedItem = null;

        _document.WidthMm = loadedDocument.WidthMm;
        _document.HeightMm = loadedDocument.HeightMm;

        foreach (var item in _document.Items.OfType<INotifyPropertyChanged>().ToList())
        {
            item.PropertyChanged -= Item_PropertyChanged;
        }

        _document.Items.Clear();
        foreach (var item in loadedDocument.Items)
        {
            _document.Items.Add(item);
        }

        UpdatePreview();
        UpdatePrintButtonState();
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
