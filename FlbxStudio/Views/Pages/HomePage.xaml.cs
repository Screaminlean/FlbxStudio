using FlbxStudio.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace FlbxStudio.Views.Pages
{
    public partial class HomePage : INavigableView<HomeViewModel>
    {
        public HomeViewModel ViewModel { get; }

        public HomePage(HomeViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

        }

    }
}
