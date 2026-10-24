using FlbxStudio.Helpers;
using FlbxStudio.Models;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wpf.Ui.Controls;

namespace FlbxStudio.Services
{
    /// <summary>
    /// Provides functionality for loading and saving user configuration settings.
    /// This service manages persistent user preferences across application sessions.
    /// </summary>
    /// <remarks>
    /// Configuration is stored in JSON format in the user's application data folder.
    /// The service provides:
    /// <list type="bullet">
    /// <item><description>Automatic handling of configuration schema changes</description></item>
    /// <item><description>Detection and removal of obsolete settings</description></item>
    /// <item><description>Automatic addition of new settings with default values</description></item>
    /// <item><description>Error handling with user feedback through SnackBar notifications</description></item>
    /// <item><description>Detailed debug logging of configuration changes</description></item>
    /// </list>
    /// </remarks>
    public class UserConfigService
    {
        /// <summary>
        /// The path to the user's application data folder.
        /// </summary>
        /// <remarks>
        /// Uses Environment.SpecialFolder.ApplicationData to ensure proper placement across different Windows versions.
        /// Typically resolves to %APPDATA% (e.g., C:\Users\Username\AppData\Roaming).
        /// </remarks>
        private static string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// The directory where the user configuration file is stored.
        /// </summary>
        /// <remarks>
        /// Created as a subdirectory under AppData using the application name from HelperFunctions.AppName.
        /// </remarks>
        private static string _configDir = Path.Combine(_appDataPath, HelperFunctions.AppName);

        /// <summary>
        /// The full path to the user configuration file.
        /// </summary>
        /// <remarks>
        /// Stored as 'UserConfig.json' within the configuration directory.
        /// </remarks>
        private static string _configPath = Path.Combine(_configDir, "UserConfig.json");

        /// <summary>
        /// Gets or sets the theme preference for the user interface.
        /// </summary>
        /// <value>
        /// A string representing the theme name. Default value is "Dark".
        /// Valid values include "Dark" and "Light".
        /// </value>
        /// <remarks>
        /// This setting affects the overall appearance of the application.
        /// Changes to this value require saving the configuration to persist between sessions.
        /// </remarks>
        public string Theme { get; set; } = "Dark";

        /// <summary>
        /// Gets or sets the UI language code for localization.
        /// </summary>
        public string Language { get; set; } = "en";

        #region Add more configuration properties here
        // e.g.
        // public int MaxItems { get; set; } = 100;

        /// <summary>
        /// Gets or sets the default user library root path used for file browsing.
        /// </summary>
        public string UserLibraryPath { get; set; } = "C:\\EBpro\\library\\picture";
        #endregion

        /// <summary>
        /// Gets the default JSON serialization options used throughout the service.
        /// </summary>
        /// <returns>A JsonSerializerOptions instance with standardized settings.</returns>
        private static JsonSerializerOptions GetSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Validates if a string contains valid JSON.
        /// </summary>
        /// <param name="strInput">The string to validate.</param>
        /// <returns>True if the string is valid JSON, false otherwise.</returns>
        private static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) return false;
            try
            {
                JsonDocument.Parse(strInput);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Loads the user configuration from disk.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="UserConfigService"/> containing the loaded configuration.
        /// If the configuration file does not exist or deserialization fails, returns a default configuration.
        /// </returns>
        /// <remarks>
        /// This method performs the following operations:
        /// <list type="bullet">
        /// <item><description>Creates the configuration directory if it doesn't exist</description></item>
        /// <item><description>Creates a default configuration file if none exists</description></item>
        /// <item><description>Validates JSON syntax and structure</description></item>
        /// <item><description>Verifies root element is a valid JSON object</description></item>
        /// <item><description>Detects and logs missing properties</description></item>
        /// <item><description>Detects and logs obsolete properties</description></item>
        /// <item><description>Sets default values for missing or invalid properties</description></item>
        /// <item><description>Falls back to default configuration on any error</description></item>
        /// </list>
        /// The method is thread-safe for reading operations.
        /// </remarks>
        public static UserConfigService Load()
        {
            if (!Directory.Exists(_configDir))
                Directory.CreateDirectory(_configDir);

            if (!File.Exists(_configPath))
            {
                // Create default config if not exists
                var defaultConfig = new UserConfigService();
                File.WriteAllText(_configPath, JsonSerializer.Serialize(defaultConfig, GetSerializerOptions()));
                return defaultConfig;
            }

            try
            {
                string json = File.ReadAllText(_configPath);

                // First validate if the JSON is valid
                if (!IsValidJson(json))
                {
                    Debug.WriteLine("Invalid JSON detected in configuration file. Creating new default configuration.");
                    var defaultConfig = new UserConfigService();
                    File.WriteAllText(_configPath, JsonSerializer.Serialize(defaultConfig, GetSerializerOptions()));
                    return defaultConfig;
                }

                using (JsonDocument jsonDoc = JsonDocument.Parse(json))
                {
                    // Validate root is an object
                    if (jsonDoc.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        Debug.WriteLine("Invalid JSON structure: Root element is not an object. Creating new default configuration.");
                        var defaultConfig = new UserConfigService();
                        File.WriteAllText(_configPath, JsonSerializer.Serialize(defaultConfig, GetSerializerOptions()));
                        return defaultConfig;
                    }

                    var jsonProperties = jsonDoc.RootElement
                        .EnumerateObject()
                        .Select(p => p.Name)
                        .ToHashSet();

                    var classProperties = typeof(UserConfigService)
                        .GetProperties()
                        .Select(p => p.Name)
                        .ToHashSet();

                    // Rest of your existing property checking logic...
                    var obsoleteProperties = jsonProperties.Except(classProperties).ToList();
                    var missingProperties = classProperties.Except(jsonProperties).ToList();

                    if (obsoleteProperties.Any())
                    {
                        Debug.WriteLine("Detected obsolete properties in user configuration file:");
                        foreach (var prop in obsoleteProperties)
                        {
                            Debug.WriteLine($"- Obsolete property: {prop}");
                        }
                    }

                    if (missingProperties.Any())
                    {
                        Debug.WriteLine("Detected missing properties in user configuration file:");
                        foreach (var prop in missingProperties)
                        {
                            Debug.WriteLine($"- Missing property: {prop}");
                        }
                    }

                    // Try to deserialize with validation of required properties
                    var config = JsonSerializer.Deserialize<UserConfigService>(json, GetSerializerOptions());

                    if (config != null)
                    {
                        var defaultConfig = new UserConfigService();
                        bool hasChanges = false;

                        foreach (var property in typeof(UserConfigService).GetProperties())
                        {
                            var defaultValue = property.GetValue(defaultConfig);
                            var loadedValue = property.GetValue(config);

                            if (missingProperties.Contains(property.Name) ||
                                loadedValue == null ||
                                (property.PropertyType.IsValueType && loadedValue.Equals(Activator.CreateInstance(property.PropertyType))))
                            {
                                property.SetValue(config, defaultValue);
                                Debug.WriteLine($"- Setting default value for property: {property.Name} = {defaultValue}");
                                hasChanges = true;
                            }
                        }

                        if (hasChanges || obsoleteProperties.Any())
                        {
                            Debug.WriteLine("Saving updated user configuration file");
                            File.WriteAllText(_configPath, JsonSerializer.Serialize(config, GetSerializerOptions()));
                        }

                        return config;
                    }
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"JSON parsing error in configuration file: {ex.Message}");
                Debug.WriteLine("Creating new default configuration.");
                var defaultConfig = new UserConfigService();
                File.WriteAllText(_configPath, JsonSerializer.Serialize(defaultConfig, GetSerializerOptions()));
                return defaultConfig;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load user configuration: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }

            return new UserConfigService();
        }

        /// <summary>
        /// Saves the specified user configuration to disk and notifies the user of the operation result.
        /// </summary>
        /// <param name="config">The <see cref="UserConfigService"/> instance to save.</param>
        /// <param name="loc">The localization service used for user notifications.</param>
        /// <remarks>
        /// This method:
        /// <list type="bullet">
        /// <item><description>Creates the configuration directory if needed</description></item>
        /// <item><description>Serializes the configuration to JSON with indentation</description></item>
        /// <item><description>Sends success/failure notifications via SnackBar messages</description></item>
        /// <item><description>Logs errors to Debug output on failure</description></item>
        /// </list>
        /// The operation is wrapped in a try-catch block to handle potential I/O errors.
        /// </remarks>
        /// <exception cref="Exception">
        /// The exception is caught and handled internally, with error details shown to the user via SnackBar.
        /// </exception>
        public static void Save(UserConfigService config, ILocalizationService loc)
        {
            if (!Directory.Exists(_configDir))
                Directory.CreateDirectory(_configDir);

            try
            {
                File.WriteAllText(_configPath, JsonSerializer.Serialize(config, GetSerializerOptions()));

                WeakReferenceMessenger.Default.Send(new SnackBarChangeMessage(
                    new SnackBarMessage
                    {
                        Title = loc.GetString("MessageSuccessTitle"),
                        Message = loc.GetString("MessageUserSettingsSaved"),
                        Appearance = ControlAppearance.Success,
                        Icon = new SymbolIcon(SymbolRegular.ArrowSync24),
                        TimeOut = TimeSpan.FromSeconds(3)
                    }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save user configuration: {ex.Message}");
                WeakReferenceMessenger.Default.Send(new SnackBarChangeMessage(
                    new SnackBarMessage
                    {
                        Title = loc.GetString("MessageErrorTitle"),
                        Message = $"{loc.GetString("MessageUserSettingsSaveError")}, {ex.Message}",
                        Appearance = ControlAppearance.Danger,
                        Icon = new SymbolIcon(SymbolRegular.ArrowSyncOff20),
                        TimeOut = TimeSpan.FromSeconds(3)
                    }));
            }
        }
    }
}
