using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace FlbxStudio.Services
{
    /// <summary>
    /// Provides localization services for loading, caching, and retrieving localized strings from resource files.
    /// Supports dynamic language switching and fallback mechanisms.
    /// </summary>
    public sealed class LocalizationService : ILocalizationService, INotifyPropertyChanged
    {
        /// <summary>
        /// The assembly containing embedded localization resources.
        /// </summary>
        private readonly Assembly _assembly;

        /// <summary>
        /// Relative path to the folder containing localization resource files.
        /// </summary>
        private readonly string _resourceFolderRelative;

        /// <summary>
        /// Thread-safe cache mapping language codes to their string dictionaries.
        /// </summary>
        private readonly ConcurrentDictionary<string, IDictionary<string, string>> _cache;

        /// <summary>
        /// Synchronization object for thread-safe operations.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// The currently active language code.
        /// </summary>
        private string _currentLanguage;

        /// <summary>
        /// List of available language codes.
        /// </summary>
        private readonly List<string> _availableLanguages;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc/>
        public string CurrentLanguage => _currentLanguage;

        /// <inheritdoc/>
        public IReadOnlyList<string> AvailableLanguages => _availableLanguages.AsReadOnly();

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationService"/> class.
        /// Loads the default language and scans for additional available languages.
        /// </summary>
        /// <param name="resourceFolderRelative">Relative path to the localization resources folder.</param>
        /// <param name="resourceAssembly">Assembly containing embedded resources (optional).</param>
        /// <param name="defaultLanguage">Default language code to load (defaults to "en").</param>
        public LocalizationService(
            string resourceFolderRelative = "Resources/Localization",
            Assembly? resourceAssembly = null,
            string defaultLanguage = "en")
        {
            _assembly = resourceAssembly ?? Assembly.GetExecutingAssembly();
            _resourceFolderRelative = resourceFolderRelative ?? "Resources/Localization";
            _currentLanguage = defaultLanguage ?? "en";
            _cache = new ConcurrentDictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            _availableLanguages = new List<string>();

            Debug.WriteLine($"LocalizationService: Initializing with folder {resourceFolderRelative}");

            // Ensure the default language is loaded into the cache.
            EnsureLoaded(defaultLanguage);

            // Scan the resource folder for additional language files.
            ScanFolderForLanguages();

            // If the default language was not found, attempt to load it from embedded resources.
            if (!_availableLanguages.Contains(defaultLanguage))
            {
                Debug.WriteLine($"LocalizationService: Default language {defaultLanguage} not found in available languages");
                EnsureLoadedEmbeddedFallback(defaultLanguage);
            }
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        private void OnPropertyChanged(string propertyName)
        {
            Debug.WriteLine($"LocalizationService: Property changed {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <inheritdoc/>
        public void SetLanguage(string languageCode)
        {
            ArgumentNullException.ThrowIfNull(languageCode);

            // If the requested language is already active, do nothing.
            if (_currentLanguage.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                return;

            lock (_lock)
            {
                // Ensure the requested language is loaded.
                EnsureLoaded(languageCode);
                _currentLanguage = languageCode;
            }

            Debug.WriteLine($"LocalizationService: Language changed to {languageCode}");
            OnPropertyChanged(nameof(CurrentLanguage));
            OnPropertyChanged("Item[]"); // Notify indexer binding clients.
        }

        /// <summary>
        /// Adds a language code to the list of available languages if not already present.
        /// </summary>
        /// <param name="languageCode">The language code to add.</param>
        private void AddAvailableLanguage(string languageCode)
        {
            lock (_lock)
            {
                if (!_availableLanguages.Contains(languageCode))
                {
                    _availableLanguages.Add(languageCode);
                    Debug.WriteLine($"LocalizationService: Added language {languageCode}");
                }
            }
        }

        /// <inheritdoc/>
        public bool TryGetString(string key, out string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = string.Empty;
                return false;
            }

            Debug.WriteLine($"LocalizationService: Attempting to get string for key {key} in language {_currentLanguage}");

            // Try to get the value from the current language dictionary.
            if (_cache.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out value!))
            {
                Debug.WriteLine($"LocalizationService: Found value for key {key}: {value}");
                return true;
            }

            // Fallback to English if not found in current language.
            if (!_currentLanguage.Equals("en", StringComparison.OrdinalIgnoreCase)
                && _cache.TryGetValue("en", out var enDict)
                && enDict.TryGetValue(key, out value!))
            {
                Debug.WriteLine($"LocalizationService: Found fallback value for key {key}: {value}");
                return true;
            }

            // If not found, return the key itself as a fallback.
            Debug.WriteLine($"LocalizationService: No value found for key {key}");
            value = key;
            return false;
        }

        /// <inheritdoc/>
        public string GetString(string key)
        {
            TryGetString(key, out var value);
            return value;
        }

        /// <inheritdoc/>
        public void RescanFolder()
        {
            Debug.WriteLine("LocalizationService: Rescanning folder for language files");
            ScanFolderForLanguages();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Attempts to load a language dictionary from embedded resources as a fallback.
        /// </summary>
        /// <param name="languageCode">The language code to load.</param>
        private void EnsureLoadedEmbeddedFallback(string languageCode)
        {
            try
            {
                // Find the embedded resource matching the language code.
                var embeddedName = _assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.IndexOf("Resources.Localization", StringComparison.OrdinalIgnoreCase) >= 0
                                         && n.EndsWith($".{languageCode}.json", StringComparison.OrdinalIgnoreCase));
                if (embeddedName != null)
                {
                    using var s = _assembly.GetManifestResourceStream(embeddedName);
                    if (s != null)
                    {
                        using var r = new StreamReader(s);
                        var json = r.ReadToEnd();
                        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                        _cache.TryAdd(languageCode, dict);
                        AddAvailableLanguage(languageCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LocalizationService: Failed to load embedded resource for {languageCode}: {ex.Message}");
            }
        }

        /// <summary>
        /// Scans the resource folder for available language files and loads them.
        /// </summary>
        private void ScanFolderForLanguages()
        {
            try
            {
                var asmFolder = Path.GetDirectoryName(_assembly.Location) ?? AppContext.BaseDirectory;
                var folder = Path.Combine(asmFolder!, _resourceFolderRelative);
                if (!Directory.Exists(folder))
                {
                    Debug.WriteLine($"LocalizationService: Resource folder not found: {folder}");
                    return;
                }

                // Enumerate all .json files in the folder and load each as a language dictionary.
                foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly))
                {
                    var lang = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrWhiteSpace(lang)) continue;
                    EnsureLoaded(lang, file);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LocalizationService: Failed to scan folder: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a language dictionary from a file or embedded resource, if not already cached.
        /// </summary>
        /// <param name="languageCode">The language code to load.</param>
        /// <param name="explicitFilePath">Optional explicit file path to load from.</param>
        private void EnsureLoaded(string languageCode, string? explicitFilePath = null)
        {
            if (_cache.ContainsKey(languageCode)) return;

            Debug.WriteLine($"LocalizationService: Loading language {languageCode}");

            try
            {
                var loaded = false;
                IDictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // Try to load from an explicit file path if provided.
                if (!string.IsNullOrEmpty(explicitFilePath) && File.Exists(explicitFilePath))
                {
                    try
                    {
                        var text = File.ReadAllText(explicitFilePath);
                        var loadedDict = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
                        if (loadedDict != null)
                        {
                            dict = new Dictionary<string, string>(loadedDict, StringComparer.OrdinalIgnoreCase);
                            loaded = true;
                            Debug.WriteLine($"LocalizationService: Loaded from explicit path: {explicitFilePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"LocalizationService: Failed to load from explicit path: {ex.Message}");
                    }
                }

                // If not loaded, try to load from the default resource folder.
                if (!loaded)
                {
                    var asmFolder = Path.GetDirectoryName(_assembly.Location) ?? AppContext.BaseDirectory;
                    var filePath = Path.Combine(asmFolder, _resourceFolderRelative, $"{languageCode}.json");

                    Debug.WriteLine($"LocalizationService: Attempting to load from: {filePath}");

                    if (File.Exists(filePath))
                    {
                        var text = File.ReadAllText(filePath);
                        var loadedDict = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
                        if (loadedDict != null)
                        {
                            dict = new Dictionary<string, string>(loadedDict, StringComparer.OrdinalIgnoreCase);
                            loaded = true;
                            Debug.WriteLine($"LocalizationService: Loaded from relative path: {filePath}");
                        }
                    }
                }

                // If still not loaded, try to load from embedded resources.
                if (!loaded)
                {
                    EnsureLoadedEmbeddedFallback(languageCode);
                    return;
                }

                // Add the loaded dictionary to the cache and update available languages.
                _cache.TryAdd(languageCode, dict);
                AddAvailableLanguage(languageCode);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LocalizationService: Failed to load language {languageCode}: {ex.Message}");
                _cache.TryAdd(languageCode, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}
