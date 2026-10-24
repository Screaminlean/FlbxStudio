namespace FlbxStudio.Models;

// Represents an individual graphic state frame
public partial class HmiPictureState : ObservableObject
{
    [ObservableProperty] private short _stateIndex;
    [ObservableProperty] private byte[] _rawImageData = Array.Empty<byte>();
    [ObservableProperty] private string _imageFormat = "PNG";
}