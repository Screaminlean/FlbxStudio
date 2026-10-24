using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Markup;
using System.Windows.Threading;

namespace FlbxStudio.Extensions
{
    /// <summary>
    /// A XAML markup extension that provides localized strings and updates them when the language changes.
    /// </summary>
    /// <remarks>
    /// This extension can be used in XAML to bind UI elements to localized resources.
    /// It automatically updates the bound value when the application's language changes,
    /// ensuring that UI text reflects the current language selection.
    /// </remarks>
    [MarkupExtensionReturnType(typeof(object))]
    public sealed class LocalizeExtension : MarkupExtension, IDisposable
    {
        /// <summary>
        /// Synchronization object for thread safety when updating target properties.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// Indicates whether the extension has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Gets or sets the localization key used to retrieve the localized string.
        /// </summary>
        /// <remarks>
        /// This key should correspond to a resource entry in the localization service.
        /// </remarks>
        [ConstructorArgument("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the format arguments for the localized string.
        /// </summary>
        /// <remarks>
        /// If the localized string contains format placeholders, these arguments will be used.
        /// </remarks>
        public object[] Args { get; set; } = Array.Empty<object>();

        /// <summary>
        /// Reference to the localization service used to retrieve localized strings.
        /// </summary>
        private ILocalizationService? _service;

        /// <summary>
        /// Event handler for property change notifications from the localization service.
        /// </summary>
        private PropertyChangedEventHandler? _handler;

        /// <summary>
        /// Weak reference to the target object whose property is being set by this extension.
        /// </summary>
        private WeakReference? _targetObjectRef;

        /// <summary>
        /// Reference to the target property being set by this extension.
        /// </summary>
        private object? _targetProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizeExtension"/> class.
        /// </summary>
        public LocalizeExtension() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizeExtension"/> class with a specified localization key.
        /// </summary>
        /// <param name="key">The localization key to use for retrieving the localized string.</param>
        public LocalizeExtension(string key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <summary>
        /// Provides the value for the target property in XAML.
        /// </summary>
        /// <param name="serviceProvider">Service provider for markup extension services.</param>
        /// <returns>The localized string, formatted if arguments are provided.</returns>
        /// <remarks>
        /// This method resolves the localization service, subscribes to language change notifications,
        /// and returns the localized value for the specified key.
        /// </remarks>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            Debug.WriteLine($"LocalizeExtension: ProvideValue called for key {Key}");

            // Retrieve the target object and property from the service provider
            var pvt = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (pvt == null)
            {
                Debug.WriteLine("LocalizeExtension: IProvideValueTarget service not found");
                return Key;
            }

            // Store target object as weak reference to prevent memory leaks
            _targetObjectRef = pvt.TargetObject != null ? new WeakReference(pvt.TargetObject) : null;
            _targetProperty = pvt.TargetProperty;

            Debug.WriteLine($"LocalizeExtension: Target object type: {_targetObjectRef?.Target?.GetType().Name}");
            Debug.WriteLine($"LocalizeExtension: Target property: {_targetProperty}");

            // Clean up old event handler if it exists
            UnsubscribeFromPropertyChanged();

            // Attempt to resolve the localization service from application resources or service provider
            _service = TryGetFromApplicationResources() ?? ResolveFromServiceProvider(serviceProvider);

            if (_service == null)
            {
                Debug.WriteLine("LocalizeExtension: No localization service found");
                return Key;
            }

            // Subscribe to property change notifications if supported
            if (_service is INotifyPropertyChanged notifyPropertyChanged)
            {
                _handler = (s, e) =>
                {
                    // Update target when language or resource changes
                    if (e.PropertyName == "Item[]" || e.PropertyName == nameof(ILocalizationService.CurrentLanguage))
                    {
                        Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, UpdateTarget);
                    }
                };
                notifyPropertyChanged.PropertyChanged += _handler;
                Debug.WriteLine("LocalizeExtension: Subscribed to PropertyChanged events");
            }

            // Resolve and return the localized string
            var result = ResolveString();
            Debug.WriteLine($"LocalizeExtension: Resolved value: {result}");
            return result;
        }

        /// <summary>
        /// Unsubscribes from property change notifications to prevent memory leaks.
        /// </summary>
        private void UnsubscribeFromPropertyChanged()
        {
            if (_service is INotifyPropertyChanged notifyPropertyChanged && _handler != null)
            {
                try
                {
                    notifyPropertyChanged.PropertyChanged -= _handler;
                }
                catch (Exception)
                {
                    // Ignore errors during unsubscription
                }
                _handler = null;
            }
        }

        /// <summary>
        /// Attempts to resolve the localization service from the provided service provider.
        /// </summary>
        /// <param name="serviceProvider">The service provider from XAML.</param>
        /// <returns>The resolved <see cref="ILocalizationService"/> instance, or null if not found.</returns>
        private ILocalizationService? ResolveFromServiceProvider(IServiceProvider serviceProvider)
        {
            try
            {
                // Try to get the service from nested service providers
                if (serviceProvider.GetService(typeof(IServiceProvider)) is IServiceProvider sp)
                {
                    if (sp.GetService(typeof(ILocalizationService)) is ILocalizationService svc)
                        return svc;
                }

                // Try to get the service directly
                if (serviceProvider.GetService(typeof(ILocalizationService)) is ILocalizationService direct)
                    return direct;
            }
            catch (Exception)
            {
                // Swallow exceptions during service resolution
            }
            return null;
        }

        /// <summary>
        /// Attempts to retrieve the localization service from the application's resources.
        /// </summary>
        /// <returns>The <see cref="ILocalizationService"/> instance if found; otherwise, null.</returns>
        private ILocalizationService? TryGetFromApplicationResources()
        {
            try
            {
                var resources = Application.Current?.Resources;
                if (resources == null) return null;

                return resources.Values.OfType<ILocalizationService>().FirstOrDefault();
            }
            catch (Exception)
            {
                // Swallow exceptions during resource lookup
            }
            return null;
        }

        /// <summary>
        /// Resolves the localized string for the current key and applies formatting if arguments are provided.
        /// </summary>
        /// <returns>The formatted localized string, or the key if not found.</returns>
        private object ResolveString()
        {
            if (_service == null || string.IsNullOrEmpty(Key))
                return Key ?? string.Empty;

            var raw = _service.GetString(Key);
            try
            {
                // Apply string formatting if arguments are provided
                return Args.Length > 0 ? string.Format(raw, Args) : raw;
            }
            catch (FormatException)
            {
                // Return raw string if formatting fails
                return raw;
            }
        }

        /// <summary>
        /// Updates the target property with the latest localized value.
        /// </summary>
        /// <remarks>
        /// This method is called when the language or resource changes.
        /// It uses reflection and dispatcher to safely update the UI element.
        /// </remarks>
        private void UpdateTarget()
        {
            if (_isDisposed || _targetObjectRef == null || !_targetObjectRef.IsAlive)
                return;

            try
            {
                var targetObject = _targetObjectRef.Target;
                if (targetObject == null)
                    return;

                var newValue = ResolveString();

                lock (_lock)
                {
                    // Update DependencyProperty if target is a DependencyObject
                    if (targetObject is DependencyObject dobj && _targetProperty is DependencyProperty dprop)
                    {
                        dobj.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => dobj.SetValue(dprop, newValue));
                        return;
                    }

                    // Update property via reflection if possible
                    if (_targetProperty is PropertyInfo pInfo && pInfo.CanWrite)
                    {
                        pInfo.SetValue(targetObject, newValue);
                        return;
                    }

                    // Update property by name if target property is a string
                    if (_targetProperty is string propName)
                    {
                        var prop = targetObject.GetType().GetProperty(propName,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (prop?.CanWrite == true)
                            prop.SetValue(targetObject, newValue);
                    }
                }
            }
            catch (Exception)
            {
                // Swallow exceptions to avoid XAML render failures
            }
        }

        /// <summary>
        /// Disposes the extension, unsubscribing from events and releasing references.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            UnsubscribeFromPropertyChanged();
            _targetObjectRef = null;
            _targetProperty = null;
            _service = null;
            _isDisposed = true;
        }
    }
}
