using FlbxStudio.Helpers;
using System.Collections.ObjectModel;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace FlbxStudio.ViewModels.Windows
{
    /// <summary>
    /// View model for the main window of the application. Handles navigation, dialogs, and notifications.
    /// This class is marked as partial to support MVVM Community Toolkit source generators.
    /// </summary>
    /// <remarks>
    /// Inherits from ObservableObject to provide property change notifications.
    /// Uses MVVM Community Toolkit's [ObservableProperty] attribute for automatic property implementation.
    /// Integrates with WPF UI's navigation, dialog, and snackbar services.
    /// </remarks>
    public partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// Gets or sets the application title displayed in the window's title bar.
        /// Generated property will be named ApplicationTitle.
        /// </summary>
        /// <remarks>
        /// Uses the application name from HelperFunctions.AppName as the default value.
        /// The [ObservableProperty] attribute generates the public property and change notification.
        /// </remarks>
        [ObservableProperty]
        private string _applicationTitle = HelperFunctions.AppName;

        /// <summary>
        /// Gets or sets the collection of main navigation menu items.
        /// Generated property will be named MenuItems.
        /// </summary>
        /// <remarks>
        /// Contains NavigationViewItems for primary navigation destinations:
        /// - Home page with home icon
        /// - Data page with histogram icon
        /// Each item specifies its target page type for navigation.
        /// </remarks>
        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new();

        /// <summary>
        /// Gets or sets the collection of footer navigation menu items.
        /// Generated property will be named FooterMenuItems.
        /// </summary>
        /// <remarks>
        /// Contains NavigationViewItems displayed at the bottom of the navigation pane:
        /// - Settings page with settings icon
        /// These items are typically for application-wide functions.
        /// </remarks>
        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new();

        /// <summary>
        /// Gets or sets the collection of system tray menu items.
        /// Generated property will be named TrayMenuItems.
        /// </summary>
        /// <remarks>
        /// Provides a collection for system tray context menu items.
        /// Initially empty, can be populated with MenuItem objects as needed.
        /// </remarks>
        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new();

        /// <summary>
        /// Gets or sets the snackbar service for displaying notifications.
        /// Generated property will be named SnackbarService.
        /// </summary>
        /// <remarks>
        /// Handles the display of temporary notifications using WPF UI's snackbar component.
        /// Service is injected through the constructor.
        /// </remarks>
        [ObservableProperty]
        private ISnackbarService _snackbarService;

        /// <summary>
        /// Gets or sets the content dialog service for displaying modal dialogs.
        /// Generated property will be named DialogService.
        /// </summary>
        /// <remarks>
        /// Manages modal dialog windows using WPF UI's dialog component.
        /// Service is injected through the constructor.
        /// </remarks>
        [ObservableProperty]
        private IContentDialogService _dialogService;

        /// <summary>
        /// Provides access to localization services for retrieving localized strings.
        /// </summary>
        private readonly ILocalizationService _loc;

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class.
        /// </summary>
        /// <param name="contentDialogService">Service for displaying modal dialogs.</param>
        /// <param name="snackbarService">Service for displaying notifications.</param>
        /// <param name="loc">Localization service for retrieving localized strings.</param>
        /// <remarks>
        /// Sets up the view model by:
        /// <list type="bullet">
        /// <item><description>Initializing dialog and snackbar services.</description></item>
        /// <item><description>Registering for SnackBarChangeMessage notifications.</description></item>
        /// <item><description>Registering for LanguageChangedMessage notifications to update menu items on language change.</description></item>
        /// </list>
        /// Uses WeakReferenceMessenger to maintain loose coupling between components.
        /// </remarks>
        public MainWindowViewModel(IContentDialogService contentDialogService, ISnackbarService snackbarService, ILocalizationService loc)
        {
            // Assign injected services to properties and fields
            DialogService = contentDialogService;
            SnackbarService = snackbarService;
            _loc = loc;

            if (_menuItems.Count <= 0) InitializeMenuItems();
            if (_footerMenuItems.Count <= 0) InitializeFooterMenuItems();

            // Register for language change messages to update menu items when the language changes
            WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (r, m) =>
            {
                // Clear and re-initialize menu items with new language
                InitializeMenuItems();
                InitializeFooterMenuItems();
            });

            // Register for snackbar notifications using weak references to prevent memory leaks
            WeakReferenceMessenger.Default.Register<SnackBarChangeMessage>(this, (r, m) =>
            {
                var message = m.Value;
                // Display snackbar notification with provided message details
                SnackbarService.Show(
                    message.Title,
                    message.Message,
                    message.Appearance,
                    message.Icon,
                    message.TimeOut
                    );
            });
        }

        /// <summary>
        /// Initializes the main navigation menu items.
        /// Adds items for Home and Data pages, using localized strings and appropriate icons.
        /// </summary>
        public void InitializeMenuItems()
        {
            // Clear and re-initialize menu items
            MenuItems.Clear();

            // Add Program navigation item
            MenuItems.Add(new NavigationViewItem()
            {
                Content = "Program",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.HomePage)
            });

            // Add User navigation item
            MenuItems.Add(new NavigationViewItem()
            {
                Content = "User",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Person24 },
                TargetPageType = typeof(Views.Pages.UserPage)
            });

            // Add PNG Lab navigation item
            MenuItems.Add(new NavigationViewItem()
            {
                Content = "PNG Lab",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Image24 },
                TargetPageType = typeof(Views.Pages.PngLabPage)
            });

        }

        /// <summary>
        /// Initializes the footer navigation menu items.
        /// Adds an item for the Settings page, using a localized string and settings icon.
        /// </summary>
        public void InitializeFooterMenuItems()
        {
            // Clear and re-initialize footer menu items 
            FooterMenuItems.Clear();

            // Add Settings navigation item to footer
            FooterMenuItems.Add(new NavigationViewItem()
            {
                Content = _loc.GetString("Settings"), // Localized label for Settings
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 }, // Settings icon
                TargetPageType = typeof(Views.Pages.SettingsPage) // Target page type for navigation
            });
        }
    }
}
