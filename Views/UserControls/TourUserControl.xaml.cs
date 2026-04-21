using System.Windows;
using System.Windows.Controls;
using TourAgency2018.Models.DTO;
using TourAgency2018.Services;
using TourAgency2018.Views.Pages;

namespace TourAgency2018.Views.UserControls
{
    public partial class TourUserControl : UserControl
    {
        public TourUserControl()
        {
            InitializeComponent();
        }

        private void DetailButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TourDTO tour)
                FrameService.Frame.Navigate(new TourDetailPage(tour.Id));
        }
    }
}
