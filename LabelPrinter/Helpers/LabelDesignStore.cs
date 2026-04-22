using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using LabelPrinter.Models;

namespace LabelPrinter.Helpers
{
    public static class LabelDesignStore
    {
        public const string Extension = ".label.json";

        private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

        public static string DesignsDirectory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LabelPrinter",
                "Designs");

        public static IReadOnlyList<string> GetSavedDesignNames()
        {
            if (!Directory.Exists(DesignsDirectory))
            {
                return Array.Empty<string>();
            }

            return Directory.EnumerateFiles(DesignsDirectory, "*" + Extension)
                .Select(GetDesignNameFromPath)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public static bool Exists(string designName)
        {
            return File.Exists(GetDesignPath(designName));
        }

        public static string GetDesignPath(string designName)
        {
            return Path.Combine(DesignsDirectory, NormalizeDesignName(designName) + Extension);
        }

        public static string Save(LabelDocument document, string designName)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            var normalizedName = NormalizeDesignName(designName);
            Directory.CreateDirectory(DesignsDirectory);

            var path = GetDesignPath(normalizedName);
            var json = JsonSerializer.Serialize(ToDto(document), JsonOptions);
            File.WriteAllText(path, json);

            return normalizedName;
        }

        public static LabelDocument Load(string designName)
        {
            var path = GetDesignPath(designName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The selected design was not found.", path);
            }

            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<LabelDesignDto>(json, JsonOptions)
                ?? throw new InvalidDataException("The design file is empty or invalid.");

            return FromDto(dto);
        }

        public static string NormalizeDesignName(string designName)
        {
            var trimmed = designName?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Enter a design name first.", nameof(designName));
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var normalized = string.Concat(trimmed.Select(ch => invalidChars.Contains(ch) ? '_' : ch))
                .Trim()
                .TrimEnd('.', ' ');

            if (normalized.Length == 0)
            {
                throw new ArgumentException("Enter a valid design name.", nameof(designName));
            }

            return IsReservedFileName(normalized) ? "_" + normalized : normalized;
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        private static string GetDesignNameFromPath(string path)
        {
            var fileName = Path.GetFileName(path);
            return fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)
                ? fileName[..^Extension.Length]
                : Path.GetFileNameWithoutExtension(fileName);
        }

        private static bool IsReservedFileName(string name)
        {
            var baseName = name.Split('.')[0];
            return baseName.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
                   baseName.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
                   baseName.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
                   baseName.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
                   IsReservedDeviceName(baseName, "COM") ||
                   IsReservedDeviceName(baseName, "LPT");
        }

        private static bool IsReservedDeviceName(string name, string prefix)
        {
            return name.Length == 4 &&
                   name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                   name[3] >= '1' &&
                   name[3] <= '9';
        }

        private static LabelDesignDto ToDto(LabelDocument document)
        {
            return new LabelDesignDto
            {
                Version = 1,
                WidthMm = document.WidthMm,
                HeightMm = document.HeightMm,
                Items = document.Items.Select(ToDto).ToList()
            };
        }

        private static PrintableItemDto ToDto(PrintableObject item)
        {
            var dto = new PrintableItemDto
            {
                Type = GetItemType(item),
                XMm = item.XMm,
                YMm = item.YMm,
                WidthMm = item.WidthMm,
                HeightMm = item.HeightMm,
                RotationDegrees = item.RotationDegrees
            };

            switch (item)
            {
                case TextPrintable text:
                    dto.Text = text.Text;
                    dto.FontFamily = text.FontFamily;
                    dto.FontSize = text.FontSize;
                    dto.HorizontalTextAlignment = text.HorizontalTextAlignment;
                    dto.VerticalTextAlignment = text.VerticalTextAlignment;
                    break;
                case ImagePrintable image:
                    dto.ImagePath = image.ImagePath;
                    dto.MaintainAspectRatio = image.MaintainAspectRatio;
                    break;
                case BarcodePrintable barcode:
                    dto.BarcodeText = barcode.BarcodeText;
                    dto.BarcodeType = barcode.BarcodeType;
                    dto.ShowBarcodeText = barcode.ShowBarcodeText;
                    break;
                case RectanglePrintable rectangle:
                    dto.StrokeThicknessMm = rectangle.StrokeThicknessMm;
                    dto.CornerRadiusMm = rectangle.CornerRadiusMm;
                    dto.LineStyle = rectangle.LineStyle;
                    break;
            }

            return dto;
        }

        private static LabelDocument FromDto(LabelDesignDto dto)
        {
            var document = new LabelDocument
            {
                WidthMm = dto.WidthMm,
                HeightMm = dto.HeightMm
            };

            foreach (var itemDto in dto.Items ?? [])
            {
                document.Items.Add(FromDto(itemDto));
            }

            return document;
        }

        private static PrintableObject FromDto(PrintableItemDto dto)
        {
            PrintableObject item;

            if (string.Equals(dto.Type, "Text", StringComparison.OrdinalIgnoreCase))
            {
                item = new TextPrintable
                {
                    Text = dto.Text ?? string.Empty,
                    FontFamily = string.IsNullOrWhiteSpace(dto.FontFamily) ? "Segoe UI" : dto.FontFamily,
                    FontSize = dto.FontSize ?? 12.0,
                    HorizontalTextAlignment = dto.HorizontalTextAlignment ?? TextAlignment.Left,
                    VerticalTextAlignment = dto.VerticalTextAlignment ?? VerticalTextAlignment.Top
                };
            }
            else if (string.Equals(dto.Type, "Image", StringComparison.OrdinalIgnoreCase))
            {
                item = new ImagePrintable
                {
                    ImagePath = dto.ImagePath ?? string.Empty,
                    MaintainAspectRatio = dto.MaintainAspectRatio ?? true
                };
            }
            else if (string.Equals(dto.Type, "Barcode", StringComparison.OrdinalIgnoreCase))
            {
                item = new BarcodePrintable
                {
                    BarcodeText = dto.BarcodeText ?? string.Empty,
                    BarcodeType = dto.BarcodeType ?? BarcodeImageType.OneD,
                    ShowBarcodeText = dto.ShowBarcodeText ?? false
                };
            }
            else if (string.Equals(dto.Type, "Rectangle", StringComparison.OrdinalIgnoreCase))
            {
                item = new RectanglePrintable
                {
                    StrokeThicknessMm = dto.StrokeThicknessMm ?? 0.25,
                    CornerRadiusMm = dto.CornerRadiusMm ?? 0.0,
                    LineStyle = dto.LineStyle ?? LineStyle.Continuous
                };
            }
            else
            {
                throw new InvalidDataException($"Unsupported printable item type '{dto.Type}'.");
            }

            item.XMm = dto.XMm;
            item.YMm = dto.YMm;
            item.WidthMm = dto.WidthMm;
            item.HeightMm = dto.HeightMm;
            item.RotationDegrees = dto.RotationDegrees;

            return item;
        }

        private static string GetItemType(PrintableObject item)
        {
            return item switch
            {
                TextPrintable => "Text",
                ImagePrintable => "Image",
                BarcodePrintable => "Barcode",
                RectanglePrintable => "Rectangle",
                _ => throw new NotSupportedException($"Unsupported printable item type '{item.GetType().Name}'.")
            };
        }

        private sealed class LabelDesignDto
        {
            public int Version { get; set; }
            public double WidthMm { get; set; }
            public double HeightMm { get; set; }
            public List<PrintableItemDto>? Items { get; set; } = new();
        }

        private sealed class PrintableItemDto
        {
            public string Type { get; set; } = string.Empty;
            public double XMm { get; set; }
            public double YMm { get; set; }
            public double WidthMm { get; set; }
            public double HeightMm { get; set; }
            public double RotationDegrees { get; set; }

            public string? Text { get; set; }
            public string? FontFamily { get; set; }
            public double? FontSize { get; set; }
            public TextAlignment? HorizontalTextAlignment { get; set; }
            public VerticalTextAlignment? VerticalTextAlignment { get; set; }

            public string? ImagePath { get; set; }
            public bool? MaintainAspectRatio { get; set; }

            public string? BarcodeText { get; set; }
            public BarcodeImageType? BarcodeType { get; set; }
            public bool? ShowBarcodeText { get; set; }

            public double? StrokeThicknessMm { get; set; }
            public double? CornerRadiusMm { get; set; }
            public LineStyle? LineStyle { get; set; }
        }
    }
}
