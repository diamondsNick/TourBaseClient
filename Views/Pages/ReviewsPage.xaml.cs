using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TourAgency2018.Services;

namespace TourAgency2018.Views.Pages
{
    public partial class ReviewsPage : Page
    {
        public ObservableCollection<VisitedHotelItem> HotelsList { get; set; } = new ObservableCollection<VisitedHotelItem>();

        public ReviewsPage()
        {
            InitializeComponent();
            DataContext = this;
            LoadHotels();
        }

        private void LoadHotels()
        {
            HotelsList.Clear();

            var userId = SessionService.User.Id;

            using (var db = DatabaseContext.GetEntities())
            {
                var completedApps = db.TourApplications
                    .Include(a => a.Tour.Hotels.Select(h => h.Country))
                    .Where(a => a.ClientId == userId && a.Status == "Выполнена")
                    .ToList();

                var hotelIds = completedApps
                    .SelectMany(a => a.Tour.Hotels)
                    .Select(h => h.Id)
                    .Distinct()
                    .ToList();

                var reviewedHotelIds = db.HotelComments
                    .Where(c => c.ClientId == userId && hotelIds.Contains(c.HotelId))
                    .Select(c => c.HotelId)
                    .ToHashSet();

                var hotels = completedApps
                    .SelectMany(a => a.Tour.Hotels)
                    .GroupBy(h => h.Id)
                    .Select(g => g.First())
                    .ToList();

                foreach (var hotel in hotels)
                {
                    var hasReview = reviewedHotelIds.Contains(hotel.Id);
                    HotelsList.Add(new VisitedHotelItem
                    {
                        HotelId = hotel.Id,
                        HotelName = hotel.Name,
                        Country = hotel.Country?.Name ?? "Не указано",
                        Stars = hotel.CountOfStars,
                        HasReview = hasReview,
                        ReviewStatus = hasReview ? "Есть отзыв" : "Нет отзыва",
                        ButtonLabel = hasReview ? "Редактировать" : "Оставить отзыв"
                    });
                }
            }

            RecordCountText.Text = $"Найдено: {HotelsList.Count}";
        }

        private void ReviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int hotelId)
                FrameService.Frame.Navigate(new ReviewEditPage(hotelId));
        }

        public class VisitedHotelItem
        {
            public int HotelId { get; set; }
            public string HotelName { get; set; }
            public string Country { get; set; }
            public int Stars { get; set; }
            public bool HasReview { get; set; }
            public string ReviewStatus { get; set; }
            public string ButtonLabel { get; set; }
        }
    }
}
