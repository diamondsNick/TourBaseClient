using System.Windows;
using System.Windows.Controls;
using TourAgency2018.Models.DTO;
using TourAgency2018.Services;
using TourAgency2018.Views.Pages;

namespace TourAgency2018.Views.UserControls
{
    public partial class HotelUserControl : UserControl
    {
        public HotelUserControl()
        {
            InitializeComponent();
        }

        private void DetailButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is HotelDTO hotel)
                FrameService.Frame.Navigate(new HotelDetailPage(hotel.Id));
        }
    }
}
