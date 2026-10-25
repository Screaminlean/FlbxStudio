using FlbxStudio.ViewModels.Pages;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Abstractions.Controls;

namespace FlbxStudio.Views.Pages
{
    /// <summary>
    /// Interaction logic for PngLabPage.xaml
    /// </summary>
    public partial class PngLabPage : INavigableView<PngLabViewModel>
    {
        /// <summary>
        /// Gets the page view model.
        /// </summary>
        public PngLabViewModel ViewModel { get; }

        public PngLabPage(PngLabViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        private void PreviewImageControl_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            var point = e.GetPosition(element);
            ViewModel.BeginPointerActionAtDisplayPoint(point, element.ActualWidth, element.ActualHeight);

            if (sender is IInputElement inputElement)
            {
                Mouse.Capture(inputElement);
            }
        }

        private void PreviewImageControl_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not FrameworkElement element || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var point = e.GetPosition(element);
            ViewModel.ContinuePointerActionAtDisplayPoint(point, element.ActualWidth, element.ActualHeight);
        }

        private void PreviewImageControl_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ViewModel.EndPointerAction();
            Mouse.Capture(null);
        }

        private void PreviewImageControl_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                return;
            }

            ViewModel.EndPointerAction();
        }
    }
}
