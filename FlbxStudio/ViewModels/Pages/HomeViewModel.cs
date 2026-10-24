using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using FlbxStudio.Models;
using Wpf.Ui.Abstractions.Controls;

namespace FlbxStudio.ViewModels.Pages;

/// <summary>
/// Handles the interaction between the WPF-UI Page workspace and the HMI FLBX service layer.
/// </summary>
public partial class HomeViewModel : ObservableObject, INavigationAware
{
    private readonly IFlbxService _flbxService;
    private readonly AppConfigService _appConfig;
    private readonly UserConfigService _userConfig;

    /// <summary>
    /// Collection of assets parsed from or staging to write to an FLBX file.
    /// Bound directly to the left-hand navigation list view.
    /// </summary>
    public ObservableCollection<HmiVectorAsset> Assets { get; set; } = new();

    /// <summary>
    /// Gets the folder entries used by the Program library folder selector.
    /// </summary>
    public ObservableCollection<LibraryFolderEntry> ProgramLibraryFolders { get; } = new();

    /// <summary>
    /// Gets the folder entries used by the User library folder selector.
    /// </summary>
    public ObservableCollection<LibraryFolderEntry> UserLibraryFolders { get; } = new();

    /// <summary>
    /// Gets the file entries shown for the selected Program library folder.
    /// </summary>
    public ObservableCollection<LibraryFileEntry> ProgramLibraryFiles { get; } = new();

    /// <summary>
    /// Gets the file entries shown for the selected User library folder.
    /// </summary>
    public ObservableCollection<LibraryFileEntry> UserLibraryFiles { get; } = new();
    private bool _suppressAutoOpen;
    private string? _currentFlbxFilePath;

    private LibraryFolderEntry? _selectedProgramLibraryFolder;

    /// <summary>
    /// Gets or sets the selected Program library folder.
    /// </summary>
    public LibraryFolderEntry? SelectedProgramLibraryFolder
    {
        get => _selectedProgramLibraryFolder;
        set
        {
            if (SetProperty(ref _selectedProgramLibraryFolder, value))
            {
                RefreshProgramLibraryFiles();
            }
        }
    }

    private LibraryFolderEntry? _selectedUserLibraryFolder;

    /// <summary>
    /// Gets or sets the selected User library folder.
    /// </summary>
    public LibraryFolderEntry? SelectedUserLibraryFolder
    {
        get => _selectedUserLibraryFolder;
        set
        {
            if (SetProperty(ref _selectedUserLibraryFolder, value))
            {
                RefreshUserLibraryFiles();
            }
        }
    }

    private LibraryFileEntry? _selectedProgramLibraryFile;

    /// <summary>
    /// Gets or sets the selected Program library file.
    /// </summary>
    public LibraryFileEntry? SelectedProgramLibraryFile
    {
        get => _selectedProgramLibraryFile;
        set
        {
            if (SetProperty(ref _selectedProgramLibraryFile, value) && value is not null && !_suppressAutoOpen)
            {
                LoadFlbx(value.FullPath);
            }
        }
    }

    private LibraryFileEntry? _selectedUserLibraryFile;

    /// <summary>
    /// Gets or sets the selected User library file.
    /// </summary>
    public LibraryFileEntry? SelectedUserLibraryFile
    {
        get => _selectedUserLibraryFile;
        set
        {
            if (SetProperty(ref _selectedUserLibraryFile, value) && value is not null && !_suppressAutoOpen)
            {
                LoadFlbx(value.FullPath);
            }
        }
    }

    /// <summary>
    /// The currently selected asset highlighted in the center workspace editor panel.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedAsset))]
    private HmiVectorAsset? _selectedAsset;

    /// <summary>
    /// Evaluates if an asset is active to drive workspace visibility triggers.
    /// </summary>
    public bool HasSelectedAsset => SelectedAsset != null;

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class using Primary Dependency Injection.
    /// </summary>
    public HomeViewModel(IFlbxService flbxService, AppConfigService appConfig, UserConfigService userConfig)
    {
        _flbxService = flbxService;
        _appConfig = appConfig;
        _userConfig = userConfig;

        RefreshLibraryEntries();
    }

    /// <summary>
    /// Handles navigation activation for the page.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnNavigatedToAsync() => Task.CompletedTask;

    /// <summary>
    /// Handles navigation deactivation and clears selection state.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnNavigatedFromAsync()
    {
        ClearLibrarySelections();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens an FLBX file from disk and parses its binary catalog structure into the workspace view layout.
    /// </summary>
    [RelayCommand]
    private void OpenFlbx()
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "Weintek Picture Library (*.flbx)|*.flbx",
            Title = "Open HMI Picture Library File",
            InitialDirectory = ResolveInitialDirectory()
        };

        if (openDialog.ShowDialog() == true)
        {
            LoadFlbx(openDialog.FileName);
        }
    }

    [RelayCommand]
    private void RefreshLibraries()
    {
        RefreshLibraryEntries();
    }

    /// <summary>
    /// Compiles and serializes the active list of single and multi-state assets back into a valid production FLBX binary stream.
    /// </summary>
    [RelayCommand]
    private void SaveFlbx()
    {
        if (Assets.Count == 0) return;

        var defaultFileName = !string.IsNullOrWhiteSpace(_currentFlbxFilePath)
            ? Path.GetFileName(_currentFlbxFilePath)
            : "CustomHMILibrary.flbx";

        var saveDialog = new SaveFileDialog
        {
            Filter = "Weintek Picture Library (*.flbx)|*.flbx",
            Title = "Save HMI Picture Library As...",
            FileName = defaultFileName,
            InitialDirectory = ResolveSaveInitialDirectory()
        };

        if (saveDialog.ShowDialog() == true)
        {
            _flbxService.SaveAsFlbx(saveDialog.FileName, Assets.ToList());
            _currentFlbxFilePath = saveDialog.FileName;
        }
    }

    /// <summary>
    /// Appends a new blank asset structure template to the local working catalog layout tracker.
    /// </summary>
    [RelayCommand]
    private void AddNewAsset()
    {
        var asset = new HmiVectorAsset
        {
            AssetName = $"Asset_Catalog_{Assets.Count}"
        };

        Assets.Add(asset);
        SelectedAsset = asset;
    }

    /// <summary>
    /// Removes the highlighted asset from the library configuration array.
    /// </summary>
    [RelayCommand]
    private void RemoveSelectedAsset()
    {
        if (SelectedAsset == null) return;

        int currentIndex = Assets.IndexOf(SelectedAsset);
        Assets.Remove(SelectedAsset);

        // Safely adjust layout focus indices downstream
        if (Assets.Count > 0)
        {
            int targetIndex = Math.Clamp(currentIndex, 0, Assets.Count - 1);
            SelectedAsset = Assets[targetIndex];
        }
        else
        {
            SelectedAsset = null;
        }
    }

    /// <summary>
    /// Imports external graphical representations and automatically stacks them as incremental operational state nodes.
    /// </summary>
    [RelayCommand]
    private void AddStateToSelectedAsset()
    {
        if (SelectedAsset == null) return;

        var openFileDialog = new OpenFileDialog
        {
            Filter = "PNG Graphics (*.png)|*.png|Bitmap Graphics (*.bmp)|*.bmp",
            Multiselect = true,
            Title = "Import Operational State Graphic Frames"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            foreach (string file in openFileDialog.FileNames)
            {
                SelectedAsset.States.Add(new HmiPictureState
                {
                    StateIndex = (short)SelectedAsset.States.Count,
                    RawImageData = File.ReadAllBytes(file),
                    ImageFormat = Path.GetExtension(file).Replace(".", "").ToUpper()
                });
            }

            // Explicit property tick alerts UI to check element dependencies
            OnPropertyChanged(nameof(SelectedAsset));
        }
    }

    /// <summary>
    /// Drops an individual operational frame sequence block from an active asset and recalculates tracking indices safely.
    /// </summary>
    [RelayCommand]
    private void RemoveState(HmiPictureState? state)
    {
        if (SelectedAsset == null || state == null) return;

        SelectedAsset.States.Remove(state);

        // Re-align operational frame layout sequences tracking sequentially for the runtime compiler engine
        for (short i = 0; i < SelectedAsset.States.Count; i++)
        {
            SelectedAsset.States[i].StateIndex = i;
        }

        OnPropertyChanged(nameof(SelectedAsset));
    }

    private void LoadFlbx(string filePath)
    {
        var parsedAssets = _flbxService.ParseFlbx(filePath);

        Assets.Clear();
        SelectedAsset = null;

        foreach (var asset in parsedAssets)
        {
            Assets.Add(asset);
        }

        if (Assets.Count > 0)
        {
            SelectedAsset = Assets[0];
        }

        _currentFlbxFilePath = filePath;
    }

    private void RefreshLibraryEntries()
    {
        _suppressAutoOpen = true;
        PopulateFolderEntries(ProgramLibraryFolders, _appConfig.EasyBuilderLibraryPath, includeNestedFolders: true);
        PopulateFolderEntries(UserLibraryFolders, _userConfig.UserLibraryPath, includeNestedFolders: false);

        SelectedProgramLibraryFolder = ProgramLibraryFolders.FirstOrDefault();
        SelectedUserLibraryFolder = UserLibraryFolders.FirstOrDefault();
        _suppressAutoOpen = false;
    }

    private void RefreshProgramLibraryFiles()
    {
        PopulateLibraryFiles(ProgramLibraryFiles, _appConfig.EasyBuilderLibraryPath, SelectedProgramLibraryFolder);
        SelectedProgramLibraryFile = ProgramLibraryFiles.FirstOrDefault();
    }

    private void RefreshUserLibraryFiles()
    {
        PopulateLibraryFiles(UserLibraryFiles, _userConfig.UserLibraryPath, SelectedUserLibraryFolder);
        SelectedUserLibraryFile = UserLibraryFiles.FirstOrDefault();
    }

    private static void PopulateFolderEntries(
        ObservableCollection<LibraryFolderEntry> target,
        string? rootPath,
        bool includeNestedFolders)
    {
        target.Clear();

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return;
        }

        target.Add(new LibraryFolderEntry("All Folders", rootPath, true));

        SearchOption searchOption = includeNestedFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (string directoryPath in Directory.EnumerateDirectories(rootPath, "*", searchOption)
            .OrderBy(Path.GetFileName))
        {
            target.Add(new LibraryFolderEntry(Path.GetFileName(directoryPath), directoryPath, false));
        }
    }

    private static void PopulateLibraryFiles(
        ObservableCollection<LibraryFileEntry> target,
        string? rootPath,
        LibraryFolderEntry? selectedFolder)
    {
        target.Clear();

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return;
        }

        if (selectedFolder is null || !Directory.Exists(selectedFolder.FullPath))
        {
            return;
        }

        SearchOption searchOption = selectedFolder.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (string filePath in Directory.EnumerateFiles(selectedFolder.FullPath, "*.flbx", searchOption)
            .OrderBy(Path.GetFileName))
        {
            target.Add(new LibraryFileEntry(Path.GetFileNameWithoutExtension(filePath), filePath));
        }
    }

    private string? ResolveInitialDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_userConfig.UserLibraryPath) && Directory.Exists(_userConfig.UserLibraryPath))
        {
            return _userConfig.UserLibraryPath;
        }

        if (!string.IsNullOrWhiteSpace(_appConfig.EasyBuilderLibraryPath) && Directory.Exists(_appConfig.EasyBuilderLibraryPath))
        {
            return _appConfig.EasyBuilderLibraryPath;
        }

        return null;
    }

    private string? ResolveSaveInitialDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_currentFlbxFilePath))
        {
            var currentDirectory = Path.GetDirectoryName(_currentFlbxFilePath);
            if (!string.IsNullOrWhiteSpace(currentDirectory) && Directory.Exists(currentDirectory))
            {
                return currentDirectory;
            }
        }

        return ResolveInitialDirectory();
    }

    private void ClearLibrarySelections()
    {
        RefreshLibraryEntries();
        SelectedAsset = null;
        Assets.Clear();
        _currentFlbxFilePath = null;
    }

    /// <summary>
    /// Represents a selectable library folder entry.
    /// </summary>
    public sealed record LibraryFolderEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryFolderEntry"/> record.
        /// </summary>
        /// <param name="name">Display name for the folder entry.</param>
        /// <param name="fullPath">Absolute folder path.</param>
        /// <param name="includeSubfolders">Indicates whether file enumeration should include subfolders.</param>
        public LibraryFolderEntry(string name, string fullPath, bool includeSubfolders)
        {
            Name = name;
            FullPath = fullPath;
            IncludeSubfolders = includeSubfolders;
        }

        /// <summary>
        /// Gets the display name for the folder entry.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the absolute directory path represented by this entry.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Gets a value indicating whether subfolders should be included when listing files.
        /// </summary>
        public bool IncludeSubfolders { get; }
    }

    /// <summary>
    /// Represents a selectable FLBX file entry.
    /// </summary>
    public sealed record LibraryFileEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryFileEntry"/> record.
        /// </summary>
        /// <param name="name">Display name for the file entry.</param>
        /// <param name="fullPath">Absolute file path.</param>
        public LibraryFileEntry(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
        }

        /// <summary>
        /// Gets the display name for the file entry.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the absolute file path represented by this entry.
        /// </summary>
        public string FullPath { get; }
    }
}