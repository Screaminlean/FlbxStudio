namespace FlbxStudio.Messages
{
    /// <summary>
    /// Message class used to notify components that the application's language has changed.
    /// </summary>
    /// <remarks>
    /// This message is typically sent via a messenger service (such as WeakReferenceMessenger)
    /// to trigger UI updates or reload localized resources when the language setting changes.
    /// </remarks>
    public class LanguageChangedMessage
    {
        /// <summary>
        /// Gets the new language code that the application should switch to.
        /// </summary>
        /// <remarks>
        /// The language code is usually in standard format (e.g., "en-US", "fr-FR").
        /// </remarks>
        public string NewLanguageCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageChangedMessage"/> class.
        /// </summary>
        /// <param name="newLanguageCode">
        /// The language code representing the new language to be applied.
        /// </param>
        public LanguageChangedMessage(string newLanguageCode) => NewLanguageCode = newLanguageCode;
    }
}
