using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using TourAgency2018.Models;
using TourAgency2018.Models.DTO;
using TourAgency2018.Services;
using TourAgency2018.ViewModels;


namespace TourAgency2018.Views.Pages
{
    public partial class ToursPage : Page, IAllowRedactPage
    {
        public ObservableCollection<Tour> OriginalToursList { get; set; } = new ObservableCollection<Tour>();
        public ObservableCollection<TourDTO> ToursList { get; set; } = new ObservableCollection<TourDTO>();
        public TourDTO SelectedTour { get; set; }
        private static readonly Regex _regex = new Regex(@"^\d+$");
        public ToursPage()
        {
            InitializeComponent();
            ToursList = new ObservableCollection<TourDTO>();
            LoadTourTypes();
            LoadTours();
            DataContext = this;

            Unloaded += (s, e) =>
            {
                ToursList.Clear();
                DataContext = null;
            };

        }
        private void LoadTourTypes()
        {
            using (var db = DatabaseContext.GetEntities())
            {
                var types = db.Types.Select(t => t.Name).ToList();
                TourTypeBox.Items.Add("Все");
                foreach (var type in types)
                    TourTypeBox.Items.Add(type);
                TourTypeBox.SelectedIndex = 0;
            }
        }

        private void LoadTours()
        {
            ToursList.Clear();

            using (var db = DatabaseContext.GetEntities())
            {
                var tours = db.Tours
                    .Include(t => t.Types)
                    .Include(t => t.Hotels.Select(h => h.Country))
                    .ToList();

                foreach (var tour in tours)
                {
                    OriginalToursList.Add(tour);
                }
            }

            FilterTours();
        }

        private void FilterTours()
        {
            if (TourTypeBox == null || ToursList == null || StartDatePicker == null || EndDatePicker == null || ShowInactiveBox == null) return;

            ToursList.Clear();

            var searchText = SearchTextBox.Text.ToLower();

            IEnumerable<Tour> tours = OriginalToursList;

            if (ShowInactiveBox.IsChecked != true)
                tours = tours.Where(t => t.IsActual);

            if (!string.IsNullOrWhiteSpace(searchText) && searchText != "поиск...")
            {
                tours = tours.Where(t =>
                    t.Name.ToLower().Contains(searchText) ||
                    t.Hotels.Any(h => h.Name.ToLower().Contains(searchText)));
            }

            decimal lowPrice = 0;

            if (BottomPricePoint != null && !string.IsNullOrWhiteSpace(BottomPricePoint.Text))
            {
                decimal.TryParse(BottomPricePoint.Text,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out lowPrice);
            }

            decimal highPrice = 0;
            if (UpperPricePoint != null && !string.IsNullOrWhiteSpace(UpperPricePoint.Text))
            {
                decimal.TryParse(UpperPricePoint.Text,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out highPrice);
            }

            if (lowPrice > 0)
                tours = tours.Where(t => t.Price > lowPrice);

            if (highPrice > 0)
                tours = tours.Where(t => t.Price < highPrice);

            var selectedType = TourTypeBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedType) && selectedType != "Все")
                tours = tours.Where(t => t.Types.Any(tt => tt.Name == selectedType));

            if (StartDatePicker.SelectedDate.HasValue)
                tours = tours.Where(t => t.StartDate >= StartDatePicker.SelectedDate.Value);

            if (EndDatePicker.SelectedDate.HasValue)
                tours = tours.Where(t => t.EndDate <= EndDatePicker.SelectedDate.Value);

            var result = tours.Select(t => new TourDTO
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Types = t.Types.Any() ? string.Join(", ", t.Types.Select(tt => tt.Name)) : "Не указано",
                Image = t.ImagePreview,
                Price = t.Price.ToString("F2") + " рублей",
                Status = t.IsActual ? "Активный" : "Неактивный",
                Tickets = t.TicketCount > 0 ? $"{t.TicketCount} шт." : "Не указано",
                Countries = $"Страна: {GetUniqueCountries(t.Hotels)}",
                Hotels = t.Hotels.Any()
                    ? $"Отели: {string.Join(", ", t.Hotels.Select(h => h.Name))}"
                    : "Отели: Не указано"
            }).ToList();

            foreach (var tour in result)
            {
                ToursList.Add(tour);
            }

            RecordCountText.Text = $"Найдено: {ToursList.Count}";
        }

        public void Add()
        {
            FrameService.Frame.Navigate(new TourEditPage());
        }

        public void Redact()
        {
            if (SelectedTour == null)
            {
                MessageBox.Show("Выберите тур для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var tour = OriginalToursList.FirstOrDefault(t => t.Id == SelectedTour.Id);
            FrameService.Frame.Navigate(new TourEditPage(tour));
        }

        public void Remove()
        {
            if (SelectedTour == null)
            {
                MessageBox.Show("Выберите тур для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = MessageBox.Show($"Удалить тур «{SelectedTour.Name}»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var tour = db.Tours
                            .Include(t => t.TourApplications)
                            .Include(t => t.Types)
                            .Include(t => t.Hotels)
                            .Include(t => t.Services)
                            .FirstOrDefault(t => t.Id == SelectedTour.Id);

                        if (tour == null) return;

                        if (tour.TourApplications.Any())
                        {
                            MessageBox.Show($"Невозможно удалить тур «{tour.Name}»: по нему есть заявки от клиентов ({tour.TourApplications.Count} шт.).",
                                "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        tour.Types.Clear();
                        tour.Hotels.Clear();
                        tour.Services.Clear();

                        db.Tours.Remove(tour);
                        db.SaveChanges();
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                DbErrorHandler.Handle(ex);
                return;
            }

            OriginalToursList.Remove(OriginalToursList.FirstOrDefault(t => t.Id == SelectedTour.Id));
            FilterTours();

            MessageBox.Show("Тур успешно удалён.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string GetUniqueCountries(IEnumerable<TourAgency2018.Models.Hotel> hotels)
        {
            var countries = hotels
                .Where(h => h.Country != null && !string.IsNullOrWhiteSpace(h.Country.Name))
                .Select(h => h.Country.Name)
                .Distinct()
                .ToList();
            return countries.Any() ? string.Join(", ", countries) : "Не указано";
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск...")
            {
                SearchTextBox.Text = "";
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTours();
        }

        private void UpperPricePoint_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTours();
        }

        private void BottomPricePoint_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTours();
        }

        private void BottomPricePoint_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !_regex.IsMatch(e.Text);
        }

        private void UpperPricePoint_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !_regex.IsMatch(e.Text);
        }

        private void TourTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTours();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            BottomPricePoint.Text = "";
            UpperPricePoint.Text = "";
            TourTypeBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            ShowInactiveBox.IsChecked = false;
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTours();
        }

        private void EndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTours();
        }

        private void ShowInactiveBox_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            FilterTours();
        }
    }
}
