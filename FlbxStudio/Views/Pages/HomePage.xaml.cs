using FlbxStudio.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace FlbxStudio.Views.Pages
{
    /// <summary>
    /// Program workspace page for browsing and editing FLBX assets.
    /// </summary>
    public partial class HomePage : INavigableView<HomeViewModel>
    {
        /// <summary>
        /// Gets the page view model.
        /// </summary>
        public HomeViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HomePage"/> class.
        /// </summary>
        /// <param name="viewModel">The injected page view model.</param>
        public HomePage(HomeViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

        }

    }
}
