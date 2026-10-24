using FlbxStudio.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace FlbxStudio.Views.Pages
{
    public partial class UserPage : INavigableView<HomeViewModel>
    {
        public HomeViewModel ViewModel { get; }

        public UserPage(HomeViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

        }

    }
}
