using CommunityToolkit.Mvvm.DependencyInjection;
using FlbxStudio.Services;
using FlbxStudio.ViewModels.Pages;
using FlbxStudio.ViewModels.Windows;
using FlbxStudio.Views.Pages;
using FlbxStudio.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.DependencyInjection;

namespace FlbxStudio
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// Defines the application entry point and configures dependency injection, theme, and global exception handling.
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// The .NET Generic Host instance used for dependency injection, configuration, and application lifetime management.
        /// </summary>
        /// <remarks>
        /// The host is configured to provide services, configuration, and logging for the application.
        /// </remarks>
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c =>
            {
                // Set the base path for configuration files to the application's base directory.
                c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory));
            })
            .ConfigureServices((context, services) =>
            {
                // Registers navigation view page provider for WPF UI navigation.
                services.AddNavigationViewPageProvider();

                // Registers the main application host service.
                services.AddHostedService<ApplicationHostService>();

                // Registers theme manipulation service.
                services.AddSingleton<IThemeService, ThemeService>();

                // Registers taskbar manipulation service.
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Registers navigation service for page navigation.
                services.AddSingleton<INavigationService, NavigationService>();

                // Registers dialog service for showing dialogs.
                services.AddSingleton<IContentDialogService, ContentDialogService>();

                // Registers snackbar service for showing transient messages.
                services.AddSingleton<ISnackbarService, SnackbarService>();

                services.AddSingleton<ILocalizationService>(sp =>
                {
                    var svc = new LocalizationService("Resources/Localization", Assembly.GetExecutingAssembly(), defaultLanguage: "en");
                    // Put into Application resources so LocalizeExtension can find it if DI resolution fails
                    try { Application.Current.Resources[typeof(ILocalizationService)] = svc; } catch { }
                    return svc;
                });

                // Loads and registers application and user configuration as singletons.
                var appConfig = AppConfigService.Load();
                var userConfig = UserConfigService.Load();
                services.AddSingleton(appConfig);
                services.AddSingleton(userConfig);

                // Registers the FLBX service for parsing and saving FLBX files.
                services.AddSingleton<IFlbxService, FlbxService>();

                // Registers the main window and its view model.
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                // Dynamically registers all page types in the Views.Pages namespace.
                var pageTypes = typeof(HomePage).Assembly.GetTypes()
                    .Where(t => t.Namespace == "FlbxStudio.Views.Pages" && t.IsClass && !t.IsAbstract);
                foreach (var pageType in pageTypes)
                    services.AddSingleton(pageType);

                // Dynamically registers all view model types in the ViewModels.Pages namespace.
                var viewModelTypes = typeof(HomeViewModel).Assembly.GetTypes()
                    .Where(t => t.Namespace == "FlbxStudio.ViewModels.Pages" && t.IsClass && !t.IsAbstract);
                foreach (var viewModelType in viewModelTypes)
                    services.AddSingleton(viewModelType);

            }).Build();

        /// <summary>
        /// Gets the application's service provider for resolving registered services.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Handles the application startup event.
        /// Initializes configuration, starts the host, and applies the user-selected theme.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Startup event data.</param>
        private async void OnStartup(object sender, StartupEventArgs e)
        {

            // Resolve AppConfig and UserConfig from dependency injection.
            var appConfig = Services.GetRequiredService<AppConfigService>();
            var userConfig = Services.GetRequiredService<UserConfigService>();
            var loc = Services.GetRequiredService<ILocalizationService>();

            loc.SetLanguage(userConfig.Language);

            // Start the host asynchronously.
            await _host.StartAsync();

            // Apply the theme based on user settings.
            var theme = userConfig.Theme;
            switch (theme)
            {
                case "Light":
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    break;
                case "High Contrast":
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast);
                    break;
                default:
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    break;
            }
        }

        /// <summary>
        /// Handles the application exit event.
        /// Stops and disposes the host.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Exit event data.</param>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            // Stop the host asynchronously.
            await _host.StopAsync();

            // Dispose the host to release resources.
            _host.Dispose();
        }

        /// <summary>
        /// Handles unhandled exceptions thrown by the application dispatcher.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Dispatcher unhandled exception event data.</param>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
            // You can log the exception or show a user-friendly message here.
        }
    }
}
