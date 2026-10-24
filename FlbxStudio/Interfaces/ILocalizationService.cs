namespace FlbxStudio.Services
{
    /// <summary>
    /// Defines a contract for localization services that manage language resources and provide localized strings.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are responsible for handling language selection,
    /// retrieving localized strings, and managing available language resources at runtime.
    /// </remarks>
    public interface ILocalizationService : IDisposable
    {
        /// <summary>
        /// Gets the code of the currently active language (e.g., "en-US", "fr-FR").
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// Gets a read-only list of all available language codes supported by the application.
        /// </summary>
        IReadOnlyList<string> AvailableLanguages { get; }

        /// <summary>
        /// Sets the application's current language to the specified language code.
        /// </summary>
        /// <param name="languageCode">The language code to switch to (e.g., "en-US").</param>
        void SetLanguage(string languageCode);

        /// <summary>
        /// Attempts to retrieve a localized string for the specified key.
        /// </summary>
        /// <param name="key">The resource key to look up.</param>
        /// <param name="value">When this method returns, contains the localized string if found; otherwise, null.</param>
        /// <returns>
        /// <c>true</c> if the string was found; otherwise, <c>false</c>.
        /// </returns>
        bool TryGetString(string key, out string value);

        /// <summary>
        /// Retrieves the localized string for the specified key.
        /// </summary>
        /// <param name="key">The resource key to look up.</param>
        /// <returns>
        /// The localized string if found; otherwise, a fallback value or the key itself.
        /// </returns>
        string GetString(string key);

        /// <summary>
        /// Rescans the runtime folder for new or updated language files.
        /// </summary>
        /// <remarks>
        /// This method allows dynamic updates to available languages and resources without restarting the application.
        /// </remarks>
        void RescanFolder();
    }
}
