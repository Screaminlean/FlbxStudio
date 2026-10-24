using FlbxStudio.ViewModels.Windows;
using FlbxStudio.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace FlbxStudio.Views.Windows
{
    /// <summary>
    /// Main window of the application implementing navigation capabilities through INavigationWindow.
    /// This serves as the root window containing navigation, dialog, and snackbar services.
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        /// <summary>
        /// Gets the view model associated with the main window.
        /// Contains application-wide properties and commands.
        /// </summary>
        public MainWindowViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// Sets up navigation, dialog hosting, and snackbar services.
        /// </summary>
        /// <param name="viewModel">The view model for the main window</param>
        /// <param name="navigationViewPageProvider">Service that provides page instances for navigation</param>
        /// <param name="navigationService">Service that handles navigation operations</param>
        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            // Configure navigation service with the root navigation control
            navigationService.SetNavigationControl(RootNavigation);

            // Set up UI service hosts, this has to be at the end of construction or it will fail!
            ViewModel.DialogService.SetDialogHost(RootContentDialog);
            ViewModel.SnackbarService.SetSnackbarPresenter(MainSnackbarPresenter);
        }

        #region INavigationWindow methods

        /// <summary>
        /// Gets the navigation view control for this window.
        /// </summary>
        /// <returns>The root navigation view control</returns>
        public INavigationView GetNavigation() => RootNavigation;

        /// <summary>
        /// Navigates to a specified page type.
        /// </summary>
        /// <param name="pageType">The type of the page to navigate to</param>
        /// <returns>True if navigation was successful; otherwise, false</returns>
        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        /// <summary>
        /// Sets the page provider service for navigation.
        /// </summary>
        /// <param name="navigationViewPageProvider">The page provider service to use</param>
        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) =>
            RootNavigation.SetPageProviderService(navigationViewPageProvider);

        /// <summary>
        /// Shows the main window.
        /// </summary>
        public void ShowWindow() => Show();

        /// <summary>
        /// Closes the main window.
        /// </summary>
        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Handles the window closed event. Ensures the application shuts down properly
        /// when the main window is closed.
        /// </summary>
        /// <param name="e">Event arguments for the closed event</param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Interface implementation of GetNavigation (marked for removal or update).
        /// Currently throws NotImplementedException as it appears to be deprecated.
        /// </summary>
        /// <returns>Never returns as it throws an exception</returns>
        /// <exception cref="NotImplementedException">Always thrown as this method is not implemented</exception>
        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the service provider for the window (marked for removal or update).
        /// Currently throws NotImplementedException as it appears to be deprecated.
        /// </summary>
        /// <param name="serviceProvider">The service provider to set</param>
        /// <exception cref="NotImplementedException">Always thrown as this method is not implemented</exception>
        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }
    }
}
