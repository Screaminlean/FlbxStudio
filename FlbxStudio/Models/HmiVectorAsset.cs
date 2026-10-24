using System.Collections.ObjectModel;

namespace FlbxStudio.Models;

// Represents the overall asset (Can contain 1 state or many states)
public partial class HmiVectorAsset : ObservableObject
{
    [ObservableProperty] private string _assetName = "New_Asset";

    // ObservableCollection updates your WPF lists automatically when frames are added/removed
    /// <summary>
    /// Gets or sets the state collection for this asset.
    /// </summary>
    public ObservableCollection<HmiPictureState> States { get; set; } = new();

    // Helper property to easily classify the asset profile type in UI DataTriggers
    /// <summary>
    /// Gets a value indicating whether this asset contains more than one state.
    /// </summary>
    public bool IsMultiState => States.Count > 1;
}