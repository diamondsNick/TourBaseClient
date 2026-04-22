using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TourAgency2018.Services;

namespace TourAgency2018.Views.Pages
{
    public partial class TourDetailPage : Page
    {
        public TourDetailPage(int tourId)
        {
            InitializeComponent();
            LoadTour(tourId);
        }

        private void LoadTour(int tourId)
        {
            using (var db = DatabaseContext.GetEntities())
            {
                var tour = db.Tours
                    .Include(t => t.Types)
                    .Include(t => t.Hotels.Select(h => h.Country))
                    .Include(t => t.Services)
                    .FirstOrDefault(t => t.Id == tourId);

                if (tour == null) return;

                if (tour.ImagePreview != null)
                    TourImage.Source = ImageService.ToBitmap(tour.ImagePreview);

                // Основная информация
                TourName.Text = tour.Name;
                TourId.Text = $"ID: {tour.Id}";
                TourStatus.Text = $"Статус: {(tour.IsActual ? "Активный" : "Неактивный")}";
                TourPrice.Text = $"Стоимость: {tour.Price:F2} руб.";
                TourTickets.Text = $"Количество мест: {(tour.TicketCount > 0 ? $"{tour.TicketCount} шт." : "Нет билетов")}";

                var start = tour.StartDate.HasValue ? tour.StartDate.Value.ToString("dd.MM.yyyy") : "Не указано";
                var end = tour.EndDate.HasValue ? tour.EndDate.Value.ToString("dd.MM.yyyy") : "Не указано";
                TourDates.Text = $"Даты: {start} — {end}";

                // Описание и типы
                TourDescription.Text = string.IsNullOrWhiteSpace(tour.Description) ? "Не указано" : tour.Description;
                TourTypes.Text = tour.Types.Any() ? string.Join(", ", tour.Types.Select(t => t.Name)) : "Не указано";

                // Отели
                if (tour.Hotels.Any())
                {
                    HotelsList.ItemsSource = tour.Hotels.Select(h => new
                    {
                        h.Name,
                        Stars = $"Звёзды: {h.CountOfStars} ★",
                        Country = $"Страна: {h.Country?.Name ?? "Не указано"}"
                    }).ToList();
                }
                else
                {
                    HotelsList.Visibility = Visibility.Collapsed;
                    NoHotelsText.Text = "Не указано";
                    NoHotelsText.Visibility = Visibility.Visible;
                }

                // Услуги
                if (tour.Services.Any())
                {
                    ServicesList.ItemsSource = tour.Services.Select(s => s.Name).ToList();
                }
                else
                {
                    ServicesList.Visibility = Visibility.Collapsed;
                    NoServicesText.Text = "Не указано";
                    NoServicesText.Visibility = Visibility.Visible;
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new ToursPage());
        }
    }
}
