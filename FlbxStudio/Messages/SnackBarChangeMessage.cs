using CommunityToolkit.Mvvm.Messaging.Messages;
using FlbxStudio.Models;

namespace FlbxStudio.Messages
{
    /// <summary>
    /// Represents a message type for broadcasting snackbar notification changes across the application.
    /// Inherits from ValueChangedMessage{SnackBarMessage} to integrate with the MVVM Community Toolkit's messaging system.
    /// </summary>
    /// <remarks>
    /// This class is part of the application's notification system and works in conjunction with:
    /// <list type="bullet">
    /// <item><description>WeakReferenceMessenger for loosely coupled communication</description></item>
    /// <item><description>ISnackbarService for displaying the notifications</description></item>
    /// <item><description>MainWindowViewModel for handling the messages</description></item>
    /// </list>
    /// Used to broadcast snackbar notifications throughout the application without direct dependencies.
    /// </remarks>
    /// <example>
    /// Sending a notification:
    /// <code>
    /// WeakReferenceMessenger.Default.Send(new SnackBarChangeMessage(
    ///     new SnackBarMessage
    ///     {
    ///         Title = "Success",
    ///         Message = "Operation completed",
    ///         Appearance = ControlAppearance.Success
    ///     }));
    /// </code>
    /// </example>
    public class SnackBarChangeMessage : ValueChangedMessage<SnackBarMessage>
    {
        /// <summary>
        /// Initializes a new instance of the SnackBarChangeMessage class with the specified snackbar message.
        /// </summary>
        /// <param name="message">The snackbar message to be broadcast across the application.</param>
        /// <remarks>
        /// The message is passed to the base ValueChangedMessage{T} class and can be accessed
        /// through the Value property by message recipients.
        /// </remarks>
        public SnackBarChangeMessage(SnackBarMessage message)
            : base(message)
        {
        }
    }
}
