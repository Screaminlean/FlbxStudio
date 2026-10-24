using FlbxStudio.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace FlbxStudio.Views.Pages
{
    /// <summary>
    /// Application settings page.
    /// </summary>
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        /// <summary>
        /// Gets the page view model.
        /// </summary>
        public SettingsViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class.
        /// </summary>
        /// <param name="viewModel">The injected page view model.</param>
        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}
