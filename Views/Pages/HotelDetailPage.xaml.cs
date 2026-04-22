using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TourAgency2018.Services;

namespace TourAgency2018.Views.Pages
{
    public partial class HotelDetailPage : Page
    {
        public HotelDetailPage(int hotelId)
        {
            InitializeComponent();
            LoadHotel(hotelId);
        }

        private void LoadHotel(int hotelId)
        {
            using (var db = DatabaseContext.GetEntities())
            {
                var hotel = db.Hotels
                    .Include(h => h.Country)
                    .Include(h => h.MealType)
                    .Include(h => h.HotelImages)
                    .Include(h => h.HotelComments.Select(c => c.User))
                    .FirstOrDefault(h => h.Id == hotelId);

                if (hotel == null) return;

                var firstImage = hotel.HotelImages.FirstOrDefault();
                if (firstImage != null)
                {
                    HotelImage.Source = ImageService.ToBitmap(firstImage.ImageSource);
                }

                HotelName.Text = hotel.Name;
                HotelId.Text = $"ID: {hotel.Id}";
                HotelCountry.Text = $"Страна: {hotel.Country?.Name ?? "Не указано"}";
                HotelStars.Text = $"Звёзды: {hotel.CountOfStars} ★";
                HotelMealType.Text = $"Тип питания: {hotel.MealType?.Name ?? "Не указано"}";

                if (hotel.HotelComments.Any())
                {
                    CommentsList.ItemsSource = hotel.HotelComments.Select(c => new
                    {
                        Author = $"{c.User?.Surname} {c.User?.Name}",
                        Date = c.CreationDate.ToString("dd.MM.yyyy"),
                        c.Text
                    }).ToList();
                }
                else
                {
                    CommentsList.Visibility = Visibility.Collapsed;
                    NoCommentsText.Visibility = Visibility.Visible;
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new HotelsPage());
        }
    }
}
