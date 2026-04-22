using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TourAgency2018.Models;
using TourAgency2018.Models.DTO;
using TourAgency2018.Services;
using TourAgency2018.ViewModels;

namespace TourAgency2018.Views.Pages
{
    public partial class HotelsPage : Page, IAllowRedactPage
    {
        public ObservableCollection<Hotel> OriginalHotelsList { get; set; } = new ObservableCollection<Hotel>();
        public ObservableCollection<HotelDTO> HotelsList { get; set; } = new ObservableCollection<HotelDTO>();
        public HotelDTO SelectedHotel { get; set; }

        public HotelsPage()
        {
            InitializeComponent();
            HotelsList = new ObservableCollection<HotelDTO>();
            DataContext = this;

            LoadFilters();
            LoadHotels();

            Unloaded += (s, e) =>
            {
                HotelsList.Clear();
                DataContext = null;
            };
        }

        private void LoadFilters()
        {
            using (var db = DatabaseContext.GetEntities())
            {
                CountryBox.Items.Add("Все");
                foreach (var c in db.Countries.OrderBy(c => c.Name).ToList())
                    CountryBox.Items.Add(c.Name);
                CountryBox.SelectedIndex = 0;

                MealTypeBox.Items.Add("Все");
                foreach (var m in db.MealTypes.OrderBy(m => m.Name).ToList())
                    MealTypeBox.Items.Add(m.Name);
                MealTypeBox.SelectedIndex = 0;
            }

            StarsBox.Items.Add("Все");
            foreach (var s in new[] { 1, 2, 3, 4, 5 })
                StarsBox.Items.Add(s.ToString());
            StarsBox.SelectedIndex = 0;
        }

        private void LoadHotels()
        {
            OriginalHotelsList.Clear();

            using (var db = DatabaseContext.GetEntities())
            {
                var hotels = db.Hotels
                    .Include(h => h.Country)
                    .Include(h => h.HotelImages)
                    .Include(h => h.MealType)
                    .ToList();

                foreach (var h in hotels)
                    OriginalHotelsList.Add(h);
            }

            InitializeFiltering();
        }

        private bool CanFilter()
        {
            if (CountryBox == null || MealTypeBox == null || StarsBox == null || HotelsList == null) return false;
            return true;
        }

        private void InitializeFiltering()
        {
            if (CanFilter())
            {
                var searchText = SearchTextBox.Text.ToLower();

                var selectedCountry = CountryBox.SelectedItem as string;               

                var selectedMealType = MealTypeBox.SelectedItem as string;
                
                var selectedStars = StarsBox.SelectedItem as string;
                

                var filteredHotels = FilterHotels(searchText,
                    selectedCountry,
                    selectedMealType,
                    selectedStars,
                    OriginalHotelsList);

                UpdateHotelsList(filteredHotels);
            }
        }

        private void UpdateHotelsList(IEnumerable<Hotel> hotels)
        {
            foreach (var h in hotels)
            {
                HotelsList.Add(new HotelDTO
                {
                    Id = h.Id,
                    Name = h.Name,
                    Country = $"Страна: {h.Country?.Name ?? "Не указано"}",
                    Stars = $"Звёзды: {h.CountOfStars} ★",
                    MealType = h.MealType != null ? $"Питание: {h.MealType.Name}" : "Питание: Не указано",
                    Image = h.HotelImages.FirstOrDefault()?.ImageSource
                });
            }

            RecordCountText.Text = $"Найдено: {HotelsList.Count}";
        }

        public IEnumerable<Hotel> FilterHotels(string searchText,
            string selectedCountry,
            string selectedMealType,
            string selectedStars,
            IEnumerable<Hotel> unfilteredHotels)
        {
            HotelsList.Clear();

            IEnumerable<Hotel> hotels = unfilteredHotels;

            if (!string.IsNullOrWhiteSpace(searchText) && searchText != "поиск...")
                hotels = hotels.Where(h => h.Name.ToLower().Contains(searchText));

            if (!string.IsNullOrEmpty(selectedCountry) && selectedCountry != "Все")
                hotels = hotels.Where(h => h.Country?.Name == selectedCountry);

            if (!string.IsNullOrEmpty(selectedMealType) && selectedMealType != "Все")
                hotels = hotels.Where(h => h.MealType?.Name == selectedMealType);

            if (!string.IsNullOrEmpty(selectedStars) && selectedStars != "Все" && int.TryParse(selectedStars, out var stars))
                hotels = hotels.Where(h => h.CountOfStars == stars);

            return hotels;
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск...")
                SearchTextBox.Text = "";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => InitializeFiltering();
        private void CountryBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => InitializeFiltering();
        private void MealTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => InitializeFiltering();
        private void StarsBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => InitializeFiltering();

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "Поиск...";
            CountryBox.SelectedIndex = 0;
            MealTypeBox.SelectedIndex = 0;
            StarsBox.SelectedIndex = 0;
        }

        public void Add()
        {
            FrameService.Frame.Navigate(new HotelEditPage());
        }

        public void Redact()
        {
            if (SelectedHotel == null)
            {
                MessageBox.Show("Выберите отель для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var hotel = OriginalHotelsList.FirstOrDefault(h => h.Id == SelectedHotel.Id);
            FrameService.Frame.Navigate(new HotelEditPage(hotel));
        }

        public void Remove()
        {
            if (SelectedHotel == null)
            {
                MessageBox.Show("Выберите отель для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить отель «{SelectedHotel.Name}»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var hotel = db.Hotels
                            .Include(h => h.Tours)
                            .Include(h => h.HotelImages)
                            .Include(h => h.HotelComments)
                            .Include(h => h.MealServingTypes)
                            .FirstOrDefault(h => h.Id == SelectedHotel.Id);

                        if (hotel == null) return;

                        if (hotel.Tours.Any())
                        {
                            MessageBox.Show($"Невозможно удалить отель «{hotel.Name}»: он используется в турах ({hotel.Tours.Count} шт.).",
                                "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        hotel.MealServingTypes.Clear();

                        db.HotelImages.RemoveRange(hotel.HotelImages.ToList());
                        db.HotelComments.RemoveRange(hotel.HotelComments.ToList());

                        db.Hotels.Remove(hotel);
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

            OriginalHotelsList.Remove(OriginalHotelsList.FirstOrDefault(h => h.Id == SelectedHotel.Id));
            InitializeFiltering();

            MessageBox.Show("Отель успешно удалён.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
