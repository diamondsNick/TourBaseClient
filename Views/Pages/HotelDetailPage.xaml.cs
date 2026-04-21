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
                    .Include(h => h.HotelComments.Select(c => c.Client))
                    .FirstOrDefault(h => h.Id == hotelId);

                if (hotel == null) return;

                var firstImage = hotel.HotelImages.FirstOrDefault();
                if (firstImage != null)
                {
                    var bmp = new BitmapImage();
                    using (var ms = new MemoryStream(firstImage.ImageSource))
                    {
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                    }
                    HotelImage.Source = bmp;
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
                        Author = $"{c.Client?.Surname} {c.Client?.Name}",
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
