using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Abstractions.Controls;

namespace FlbxStudio.ViewModels.Pages;

public partial class PngLabViewModel : ObservableObject, INavigationAware
{
    private enum PngLabTool
    {
        Pick,
        Brush,
        Eraser
    }

    private readonly AppConfigService _appConfig;
    private readonly UserConfigService _userConfig;

    private WriteableBitmap? _editableBitmap;
    private byte[]? _pixelBuffer;
    private int _stride;
    private string? _currentFilePath;
    private PngLabTool _activeTool = PngLabTool.Pick;
    private bool _isPointerDrawing;
    private int _lastDrawX = -1;
    private int _lastDrawY = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviewImage))]
    private BitmapSource? _previewImage;

    public bool HasPreviewImage => PreviewImage is not null;

    [ObservableProperty]
    private string _documentTitle = "No image loaded";

    [ObservableProperty]
    private int _newImageWidth = 32;

    [ObservableProperty]
    private int _newImageHeight = 32;

    [ObservableProperty]
    private string _selectedColorHex = "#FF000000";

    [ObservableProperty]
    private string _replacementColorHex = "#FFFF0000";

    [ObservableProperty]
    private SolidColorBrush _selectedColorBrush = new(Colors.Black);

    [ObservableProperty]
    private SolidColorBrush _replacementColorBrush = new(Colors.Red);

    [ObservableProperty]
    private string _statusMessage = "Open an existing PNG or create a new one.";

    [ObservableProperty]
    private int _brushSize = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPickToolActive))]
    [NotifyPropertyChangedFor(nameof(IsBrushToolActive))]
    [NotifyPropertyChangedFor(nameof(IsEraserToolActive))]
    private string _activeToolName = "Pick";

    public bool IsPickToolActive => _activeTool == PngLabTool.Pick;
    public bool IsBrushToolActive => _activeTool == PngLabTool.Brush;
    public bool IsEraserToolActive => _activeTool == PngLabTool.Eraser;

    public PngLabViewModel(AppConfigService appConfig, UserConfigService userConfig)
    {
        _appConfig = appConfig;
        _userConfig = userConfig;
    }

    [RelayCommand]
    private void OpenPng()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PNG image (*.png)|*.png",
            Title = "Open PNG"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadPng(dialog.FileName);
        }
    }

    [RelayCommand]
    private void CreateNewPng()
    {
        if (NewImageWidth <= 0 || NewImageHeight <= 0)
        {
            StatusMessage = "Width and height must be greater than 0.";
            return;
        }

        var width = Math.Clamp(NewImageWidth, 1, 4096);
        var height = Math.Clamp(NewImageHeight, 1, 4096);

        _stride = width * 4;
        // New byte[] defaults to 0 for BGRA, which is fully transparent.
        _pixelBuffer = new byte[_stride * height];

        _editableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        _editableBitmap.WritePixels(new Int32Rect(0, 0, width, height), _pixelBuffer, _stride, 0);

        PreviewImage = _editableBitmap;
        _currentFilePath = null;
        DocumentTitle = $"Untitled ({width}x{height})";
        StatusMessage = "Created new transparent PNG canvas.";
    }

    [RelayCommand]
    private void SavePng()
    {
        if (_editableBitmap is null)
        {
            StatusMessage = "Nothing to save. Open or create an image first.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png",
            Title = "Save PNG",
            FileName = string.IsNullOrWhiteSpace(_currentFilePath) ? "image.png" : Path.GetFileName(_currentFilePath)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        using var fs = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_editableBitmap));
        encoder.Save(fs);

        _currentFilePath = dialog.FileName;
        DocumentTitle = Path.GetFileName(dialog.FileName);
        StatusMessage = $"Saved PNG: {dialog.FileName}";
    }

    [RelayCommand]
    private void ApplyColorSwap()
    {
        if (_editableBitmap is null || _pixelBuffer is null)
        {
            StatusMessage = "Open or create an image before replacing colors.";
            return;
        }

        if (!TryParseColor(SelectedColorHex, out var source) || !TryParseColor(ReplacementColorHex, out var target))
        {
            StatusMessage = "Invalid color format. Use #RRGGBB or #AARRGGBB.";
            return;
        }

        var replaced = 0;
        for (var y = 0; y < _editableBitmap.PixelHeight; y++)
        {
            var rowStart = y * _stride;
            for (var x = 0; x < _editableBitmap.PixelWidth; x++)
            {
                var i = rowStart + (x * 4);

                var b = _pixelBuffer[i + 0];
                var g = _pixelBuffer[i + 1];
                var r = _pixelBuffer[i + 2];
                var a = _pixelBuffer[i + 3];

                if (a == source.A && r == source.R && g == source.G && b == source.B)
                {
                    _pixelBuffer[i + 0] = target.B;
                    _pixelBuffer[i + 1] = target.G;
                    _pixelBuffer[i + 2] = target.R;
                    _pixelBuffer[i + 3] = target.A;
                    replaced++;
                }
            }
        }

        _editableBitmap.WritePixels(
            new Int32Rect(0, 0, _editableBitmap.PixelWidth, _editableBitmap.PixelHeight),
            _pixelBuffer,
            _stride,
            0
        );

        PreviewImage = _editableBitmap;
        StatusMessage = replaced > 0
            ? $"Replaced {replaced} pixel(s)."
            : "No pixels matched the selected color.";
    }

    [RelayCommand]
    private void SetTool(string? toolName)
    {
        var normalized = toolName?.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "brush":
                _activeTool = PngLabTool.Brush;
                ActiveToolName = "Brush";
                StatusMessage = "Brush tool active. Drag on image to draw.";
                break;
            case "eraser":
                _activeTool = PngLabTool.Eraser;
                ActiveToolName = "Eraser";
                StatusMessage = "Eraser tool active. Drag on image to erase.";
                break;
            default:
                _activeTool = PngLabTool.Pick;
                ActiveToolName = "Pick";
                StatusMessage = "Pick tool active. Click image to sample a color.";
                break;
        }
    }

    public void BeginPointerActionAtDisplayPoint(Point point, double displayWidth, double displayHeight)
    {
        if (_editableBitmap is null || _pixelBuffer is null)
        {
            return;
        }

        if (!TryMapDisplayPointToPixel(point, displayWidth, displayHeight, out var pixelX, out var pixelY))
        {
            return;
        }

        if (_activeTool == PngLabTool.Pick)
        {
            PickColorAtDisplayPoint(point, displayWidth, displayHeight);
            return;
        }

        _isPointerDrawing = true;
        _lastDrawX = pixelX;
        _lastDrawY = pixelY;
        DrawStrokePoint(pixelX, pixelY);
        FlushPixelsToBitmap();
    }

    public void ContinuePointerActionAtDisplayPoint(Point point, double displayWidth, double displayHeight)
    {
        if (!_isPointerDrawing || _editableBitmap is null || _pixelBuffer is null)
        {
            return;
        }

        if (!TryMapDisplayPointToPixel(point, displayWidth, displayHeight, out var pixelX, out var pixelY))
        {
            return;
        }

        DrawLine(_lastDrawX, _lastDrawY, pixelX, pixelY);
        _lastDrawX = pixelX;
        _lastDrawY = pixelY;
        FlushPixelsToBitmap();
    }

    public void EndPointerAction()
    {
        _isPointerDrawing = false;
        _lastDrawX = -1;
        _lastDrawY = -1;
    }

    public void PickColorAtDisplayPoint(Point point, double displayWidth, double displayHeight)
    {
        if (_editableBitmap is null || _pixelBuffer is null)
        {
            return;
        }

        if (!TryMapDisplayPointToPixel(point, displayWidth, displayHeight, out var pixelX, out var pixelY))
        {
            return;
        }

        var idx = (pixelY * _stride) + (pixelX * 4);
        var color = Color.FromArgb(
            _pixelBuffer[idx + 3],
            _pixelBuffer[idx + 2],
            _pixelBuffer[idx + 1],
            _pixelBuffer[idx + 0]
        );

        SelectedColorHex = ToHex(color);
        SelectedColorBrush = new SolidColorBrush(color);
        StatusMessage = $"Picked color {SelectedColorHex} at ({pixelX}, {pixelY}).";
    }

    partial void OnSelectedColorHexChanged(string value)
    {
        if (TryParseColor(value, out var color))
        {
            SelectedColorBrush = new SolidColorBrush(color);
        }
    }

    partial void OnReplacementColorHexChanged(string value)
    {
        if (TryParseColor(value, out var color))
        {
            ReplacementColorBrush = new SolidColorBrush(color);
        }
    }

    partial void OnBrushSizeChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 10);
        if (clamped != value)
        {
            BrushSize = clamped;
        }
    }

    private void LoadPng(string filePath)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(filePath, UriKind.Absolute);
        image.EndInit();
        image.Freeze();

        var converted = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        converted.Freeze();

        _editableBitmap = new WriteableBitmap(converted);
        _stride = _editableBitmap.PixelWidth * 4;
        _pixelBuffer = new byte[_stride * _editableBitmap.PixelHeight];
        _editableBitmap.CopyPixels(_pixelBuffer, _stride, 0);

        PreviewImage = _editableBitmap;
        _currentFilePath = filePath;
        DocumentTitle = Path.GetFileName(filePath);
        StatusMessage = $"Loaded PNG: {filePath}";
    }

    private bool TryMapDisplayPointToPixel(Point point, double displayWidth, double displayHeight, out int pixelX, out int pixelY)
    {
        pixelX = 0;
        pixelY = 0;

        if (_editableBitmap is null || displayWidth <= 0 || displayHeight <= 0)
        {
            return false;
        }

        var imageWidth = _editableBitmap.PixelWidth;
        var imageHeight = _editableBitmap.PixelHeight;
        var imageAspect = (double)imageWidth / imageHeight;
        var displayAspect = displayWidth / displayHeight;

        double renderedWidth;
        double renderedHeight;
        double offsetX;
        double offsetY;

        if (displayAspect > imageAspect)
        {
            renderedHeight = displayHeight;
            renderedWidth = renderedHeight * imageAspect;
            offsetX = (displayWidth - renderedWidth) / 2.0;
            offsetY = 0;
        }
        else
        {
            renderedWidth = displayWidth;
            renderedHeight = renderedWidth / imageAspect;
            offsetX = 0;
            offsetY = (displayHeight - renderedHeight) / 2.0;
        }

        if (point.X < offsetX || point.X > offsetX + renderedWidth || point.Y < offsetY || point.Y > offsetY + renderedHeight)
        {
            return false;
        }

        var normalizedX = (point.X - offsetX) / renderedWidth;
        var normalizedY = (point.Y - offsetY) / renderedHeight;

        pixelX = Math.Clamp((int)(normalizedX * imageWidth), 0, imageWidth - 1);
        pixelY = Math.Clamp((int)(normalizedY * imageHeight), 0, imageHeight - 1);
        return true;
    }

    private void DrawLine(int x0, int y0, int x1, int y1)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            DrawStrokePoint(x0, y0);

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = 2 * error;
            if (e2 >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private void DrawStrokePoint(int centerX, int centerY)
    {
        if (_editableBitmap is null || _pixelBuffer is null)
        {
            return;
        }

        var radius = Math.Max(0, BrushSize - 1);
        var left = Math.Max(0, centerX - radius);
        var right = Math.Min(_editableBitmap.PixelWidth - 1, centerX + radius);
        var top = Math.Max(0, centerY - radius);
        var bottom = Math.Min(_editableBitmap.PixelHeight - 1, centerY + radius);

        var drawColor = Colors.Black;
        if (_activeTool == PngLabTool.Brush)
        {
            if (!TryParseColor(ReplacementColorHex, out drawColor))
            {
                drawColor = Colors.Red;
            }
        }
        else
        {
            drawColor = Colors.Transparent;
        }

        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                var index = (y * _stride) + (x * 4);
                _pixelBuffer[index + 0] = drawColor.B;
                _pixelBuffer[index + 1] = drawColor.G;
                _pixelBuffer[index + 2] = drawColor.R;
                _pixelBuffer[index + 3] = drawColor.A;
            }
        }
    }

    private void FlushPixelsToBitmap()
    {
        if (_editableBitmap is null || _pixelBuffer is null)
        {
            return;
        }

        _editableBitmap.WritePixels(
            new Int32Rect(0, 0, _editableBitmap.PixelWidth, _editableBitmap.PixelHeight),
            _pixelBuffer,
            _stride,
            0);

        PreviewImage = _editableBitmap;
    }

    private static bool TryParseColor(string value, out Color color)
    {
        color = default;

        try
        {
            var parsed = ColorConverter.ConvertFromString(value);
            if (parsed is Color c)
            {
                color = c;
                return true;
            }
        }
        catch
        {
            // No-op: false will be returned.
        }

        return false;
    }

    private static string ToHex(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    public Task OnNavigatedFromAsync() => Task.CompletedTask;

    public Task OnNavigatedToAsync() => Task.CompletedTask;
}
