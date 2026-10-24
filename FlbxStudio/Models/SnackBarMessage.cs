using Wpf.Ui.Controls;

namespace FlbxStudio.Models
{
    /// <summary>
    /// Represents a message to be displayed in a WPF UI snackbar notification.
    /// This class encapsulates all the necessary properties for configuring
    /// and displaying toast-style notifications in the application.
    /// </summary>
    /// <remarks>
    /// SnackBarMessage is used in conjunction with SnackBarChangeMessage and ISnackbarService
    /// to provide a consistent notification system throughout the application.
    /// Messages are typically displayed at the bottom of the window for a specified duration.
    /// </remarks>
    public class SnackBarMessage
    {
        /// <summary>
        /// Gets or sets the title text displayed in the snackbar notification.
        /// </summary>
        /// <value>
        /// The title text. Defaults to an empty string.
        /// </value>
        /// <remarks>
        /// The title is typically displayed in a more prominent style than the message.
        /// </remarks>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the main message text displayed in the snackbar notification.
        /// </summary>
        /// <value>
        /// The message text. Defaults to an empty string.
        /// </value>
        /// <remarks>
        /// This is the primary content of the notification and should provide
        /// clear, concise information to the user.
        /// </remarks>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visual appearance style of the snackbar notification.
        /// </summary>
        /// <value>
        /// A ControlAppearance value determining the notification's visual style.
        /// Defaults to ControlAppearance.Primary.
        /// </value>
        /// <remarks>
        /// Common values include:
        /// - Primary: Default style
        /// - Success: For successful operations
        /// - Danger: For errors or warnings
        /// - Info: For informational messages
        /// </remarks>
        public ControlAppearance Appearance { get; set; } = ControlAppearance.Primary;

        /// <summary>
        /// Gets or sets the icon displayed in the snackbar notification.
        /// </summary>
        /// <value>
        /// An IconElement instance. Defaults to a diagonal ticket symbol.
        /// </value>
        /// <remarks>
        /// The icon helps users quickly identify the type or purpose of the notification.
        /// You can use any SymbolIcon from the WPF UI library's symbol collection.
        /// </remarks>
        public IconElement Icon { get; set; } = new SymbolIcon(SymbolRegular.TicketDiagonal24);

        /// <summary>
        /// Gets or sets the duration for which the snackbar notification is displayed.
        /// </summary>
        /// <value>
        /// A TimeSpan value. Defaults to 3 seconds.
        /// </value>
        /// <remarks>
        /// After this duration elapses, the notification will automatically dismiss itself.
        /// Set to TimeSpan.Zero or a negative value to prevent auto-dismissal.
        /// </remarks>
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(3);
    }
}
