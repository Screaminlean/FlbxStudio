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
    /// Provides functionality for loading and saving application-wide configuration settings.
    /// This service manages persistent application settings that are shared across all users.
    /// </summary>
    /// <remarks>
    /// Configuration is stored in JSON format in the common application data folder.
    /// Unlike UserConfigService which stores per-user settings, this service manages global application settings.
    /// The service provides:
    /// <list type="bullet">
    /// <item><description>Automatic handling of configuration schema changes</description></item>
    /// <item><description>Detection and removal of obsolete settings</description></item>
    /// <item><description>Automatic addition of new settings with default values</description></item>
    /// <item><description>Error handling with user feedback through SnackBar notifications</description></item>
    /// <item><description>Detailed debug logging of configuration changes</description></item>
    /// </list>
    /// </remarks>
    public class AppConfigService
    {
        /// <summary>
        /// The path to the common application data folder.
        /// </summary>
        /// <remarks>
        /// Uses Environment.SpecialFolder.CommonApplicationData to store settings accessible to all users.
        /// Typically resolves to C:\ProgramData on Windows systems.
        /// This location requires elevated privileges for write operations.
        /// </remarks>
        private static string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        /// <summary>
        /// The directory where the application configuration file is stored.
        /// </summary>
        /// <remarks>
        /// Created as a subdirectory under CommonApplicationData using the application name.
        /// The path is constructed by combining the common app data path with the application name
        /// obtained from HelperFunctions.AppName.
        /// </remarks>
        private static string _configDir = Path.Combine(_appDataPath, HelperFunctions.AppName);

        /// <summary>
        /// The full path to the application configuration file.
        /// </summary>
        /// <remarks>
        /// Stored as 'AppConfig.json' within the configuration directory.
        /// This file contains serialized application settings in JSON format.
        /// </remarks>
        private static string _configPath = Path.Combine(_configDir, "AppConfig.json");

        /// <summary>
        /// Gets or sets a sample application setting.
        /// </summary>
        /// <value>
        /// A string representing a sample setting. Default value is "Default".
        /// This is a placeholder property - extend this class with actual application settings as needed.
        /// </value>
        /// <remarks>
        /// This is a demonstration property showing how to add configuration options.
        /// Replace or augment this with actual application settings required by your application.
        /// All properties will be automatically serialized to/from JSON.
        /// </remarks>
        #region Add more configuration properties here
        public string EasyBuilderLibraryPath { get; set; } = "C:\\EBpro\\library\\picture";
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
        /// Loads the application configuration from disk.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="AppConfigService"/> containing the loaded configuration.
        /// If the configuration file does not exist or deserialization fails, returns a default configuration.
        /// </returns>
        /// <remarks>
        /// This method performs the following operations:
        /// <list type="bullet">
        /// <item><description>Creates the configuration directory if it doesn't exist</description></item>
        /// <item><description>Creates a default configuration file if none exists</description></item>
        /// <item><description>Validates JSON syntax and structure</description></item>
        /// <item><description>Detects and handles invalid JSON configurations</description></item>
        /// <item><description>Identifies missing and obsolete properties</description></item>
        /// <item><description>Sets default values for missing or invalid properties</description></item>
        /// <item><description>Falls back to default configuration on any error</description></item>
        /// </list>
        /// The method is thread-safe for reading operations.
        /// </remarks>
        public static AppConfigService Load()
        {
            if (!Directory.Exists(_configDir))
                Directory.CreateDirectory(_configDir);

            if (!File.Exists(_configPath))
            {
                // Create default config if not exists
                var defaultConfig = new AppConfigService();
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
                    var defaultConfig = new AppConfigService();
                    File.WriteAllText(_configPath, JsonSerializer.Serialize(defaultConfig, GetSerializerOptions()));
                    return defaultConfig;
                }

                using (JsonDocument jsonDoc = JsonDocument.Parse(json))
                {
                    // Validate root is an object
                    if (jsonDoc.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        Debug.WriteLine("Invalid JSON structure: Root element is not an object. Creating new default configuration.");
                        var defaultConfig = new AppConfigService();
                        File.WriteAllText(_configPath, JsonSerializer.Serialize(defaultConfig, GetSerializerOptions()));
                        return defaultConfig;
                    }

                    var jsonProperties = jsonDoc.RootElement
                        .EnumerateObject()
                        .Select(p => p.Name)
                        .ToHashSet();

                    var classProperties = typeof(AppConfigService)
                        .GetProperties()
                        .Select(p => p.Name)
                        .ToHashSet();

                    // Rest of your existing property checking logic...
                    var obsoleteProperties = jsonProperties.Except(classProperties).ToList();
                    var missingProperties = classProperties.Except(jsonProperties).ToList();

                    if (obsoleteProperties.Any())
                    {
                        Debug.WriteLine("Detected obsolete properties in app configuration file:");
                        foreach (var prop in obsoleteProperties)
                        {
                            Debug.WriteLine($"- Obsolete property: {prop}");
                        }
                    }

                    if (missingProperties.Any())
                    {
                        Debug.WriteLine("Detected missing properties in app configuration file:");
                        foreach (var prop in missingProperties)
                        {
                            Debug.WriteLine($"- Missing property: {prop}");
                        }
                    }

                    // Try to deserialize with validation of required properties
                    var config = JsonSerializer.Deserialize<AppConfigService>(json, GetSerializerOptions());

                    if (config != null)
                    {
                        var defaultConfig = new AppConfigService();
                        bool hasChanges = false;

                        foreach (var property in typeof(AppConfigService).GetProperties())
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
                            Debug.WriteLine("Saving updated app configuration file");
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
                var defaultConfig = new AppConfigService();
                File.WriteAllText(_configPath, JsonSerializer.Serialize(defaultConfig, GetSerializerOptions()));
                return defaultConfig;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load app configuration: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }

            return new AppConfigService();
        }

        /// <summary>
        /// Saves the specified application configuration to disk and notifies the user of the operation result.
        /// </summary>
        /// <param name="config">The <see cref="AppConfigService"/> instance to save.</param>
        /// <param name="loc">Localization service used to resolve notification text.</param>
        /// <remarks>
        /// This method:
        /// <list type="bullet">
        /// <item><description>Creates the configuration directory if needed</description></item>
        /// <item><description>Serializes the configuration to JSON with indentation</description></item>
        /// <item><description>Sends success/failure notifications via SnackBar messages</description></item>
        /// <item><description>Logs errors to Debug output on failure</description></item>
        /// </list>
        /// Note: This operation requires appropriate write permissions to the CommonApplicationData folder.
        /// </remarks>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the application lacks sufficient permissions to write to the configuration directory.
        /// The exception is caught and handled internally, with error details shown to the user via SnackBar.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown when an I/O error occurs during file writing.
        /// The exception is caught and handled internally, with error details shown to the user via SnackBar.
        /// </exception>
        public static void Save(AppConfigService config, ILocalizationService loc)
        {
            if (!Directory.Exists(_configDir))
                Directory.CreateDirectory(_configDir);

            try
            {
                // Serialize and save configuration with pretty printing
                File.WriteAllText(_configPath, JsonSerializer.Serialize(config, GetSerializerOptions()));

                // Send success notification via SnackBar
                WeakReferenceMessenger.Default.Send(new SnackBarChangeMessage(
                    new SnackBarMessage
                    {
                        Title = loc.GetString("MessageSuccessTitle"),
                        Message = loc.GetString("MessageAppSettingsSaved"),
                        Appearance = ControlAppearance.Success,
                        Icon = new SymbolIcon(SymbolRegular.ArrowSync24),
                        TimeOut = TimeSpan.FromSeconds(3)
                    }
                    ));
            }
            catch (Exception ex)
            {
                // Log error and send failure notification
                Debug.WriteLine($"Failed to save app configuration: {ex.Message}");
                WeakReferenceMessenger.Default.Send(new SnackBarChangeMessage(
                    new SnackBarMessage
                    {
                        Title = loc.GetString("MessageErrorTitle"),
                        Message = $"{loc.GetString("MessageAppSettingsSaveError")}, {ex.Message}",
                        Appearance = ControlAppearance.Danger,
                        Icon = new SymbolIcon(SymbolRegular.ArrowSyncOff20),
                        TimeOut = TimeSpan.FromSeconds(3)
                    }
                    ));
            }
        }
    }
}
