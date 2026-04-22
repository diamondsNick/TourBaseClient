using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TourAgency2018.Models;
using TourAgency2018.Services;

namespace TourAgency2018.Views.Pages
{
    public partial class TourEditPage : Page
    {
        private readonly int? _tourId;
        private byte[] _imageBytes;

        private ObservableCollection<Hotel> _selectedHotels = new ObservableCollection<Hotel>();
        private ObservableCollection<Models.Type> _selectedTypes = new ObservableCollection<Models.Type>();
        private ObservableCollection<Service> _selectedServices = new ObservableCollection<Service>();

        public TourEditPage(Tour tour = null)
        {
            InitializeComponent();

            SelectedHotelsList.ItemsSource = _selectedHotels;
            SelectedTypesList.ItemsSource = _selectedTypes;
            SelectedServicesList.ItemsSource = _selectedServices;

            LoadCatalogs();

            if (tour != null && tour.Id != 0)
            {
                _tourId = tour.Id;
                PageTitle.Text = "Редактирование тура";
                FillFields(tour);
            }
            else
            {
                PageTitle.Text = "Создание тура";
            }
        }

        private void LoadCatalogs()
        {
            using (var db = DatabaseContext.GetEntities())
            {
                HotelsComboBox.ItemsSource = db.Hotels.ToList();
                TypesComboBox.ItemsSource = db.Types.ToList();
                ServicesComboBox.ItemsSource = db.Services.ToList();
            }
        }

        private void FillFields(Tour tour)
        {
            using (var db = DatabaseContext.GetEntities())
            {
                var full = db.Tours
                    .Include(t => t.Hotels)
                    .Include(t => t.Types)
                    .Include(t => t.Services)
                    .FirstOrDefault(t => t.Id == tour.Id);

                if (full == null) return;

                NameBox.Text = full.Name;
                DescriptionBox.Text = full.Description;
                PriceBox.Text = full.Price.ToString("F2");
                TicketsBox.Text = full.TicketCount.ToString();
                IsActualBox.IsChecked = full.IsActual;
                StartDateBox.SelectedDate = full.StartDate;
                EndDateBox.SelectedDate = full.EndDate;

                if (full.ImagePreview != null)
                {
                    _imageBytes = full.ImagePreview;
                    SetPreviewImage(_imageBytes);
                }

                foreach (var h in full.Hotels) _selectedHotels.Add(h);
                foreach (var t in full.Types) _selectedTypes.Add(t);
                foreach (var s in full.Services) _selectedServices.Add(s);
            }
        }

        private void SetPreviewImage(byte[] bytes)
        {
            PreviewImage.Source = ImageService.ToBitmap(bytes);
        }

        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                _imageBytes = File.ReadAllBytes(dialog.FileName);
                SetPreviewImage(_imageBytes);
            }
        }

        private void AddHotel_Click(object sender, RoutedEventArgs e)
        {
            if (HotelsComboBox.SelectedItem is Hotel hotel && !_selectedHotels.Any(h => h.Id == hotel.Id))
                _selectedHotels.Add(hotel);
        }

        private void RemoveHotel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Hotel hotel)
                _selectedHotels.Remove(hotel);
        }

        private void AddType_Click(object sender, RoutedEventArgs e)
        {
            if (TypesComboBox.SelectedItem is Models.Type type && !_selectedTypes.Any(t => t.Id == type.Id))
                _selectedTypes.Add(type);
        }

        private void RemoveType_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Models.Type type)
                _selectedTypes.Remove(type);
        }

        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            if (ServicesComboBox.SelectedItem is Service service && !_selectedServices.Any(s => s.Id == service.Id))
                _selectedServices.Add(service);
        }

        private void RemoveService_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Service service)
                _selectedServices.Remove(service);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите название тура.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceBox.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var price) || price < 0)
            {
                MessageBox.Show("Введите корректную стоимость.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TicketsBox.Text, out var tickets) || tickets < 0)
            {
                MessageBox.Show("Введите корректное количество билетов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.Configuration.AutoDetectChangesEnabled = true;

                        Tour tour;

                        if (_tourId.HasValue)
                        {
                            tour = db.Tours
                                .Include(t => t.Hotels)
                                .Include(t => t.Types)
                                .Include(t => t.Services)
                                .FirstOrDefault(t => t.Id == _tourId.Value);

                            if (tour == null) return;

                            tour.Hotels.Clear();
                            tour.Types.Clear();
                            tour.Services.Clear();
                        }
                        else
                        {
                            tour = new Tour();
                            db.Tours.Add(tour);
                        }

                        tour.Name = NameBox.Text.Trim();
                        tour.Description = DescriptionBox.Text.Trim();
                        tour.Price = price;
                        tour.TicketCount = tickets;
                        tour.IsActual = IsActualBox.IsChecked == true;
                        tour.StartDate = StartDateBox.SelectedDate;
                        tour.EndDate = EndDateBox.SelectedDate;
                        tour.ImagePreview = _imageBytes;

                        var hotelIds = _selectedHotels.Select(h => h.Id).ToList();
                        var hotels = db.Hotels.Where(h => hotelIds.Contains(h.Id)).ToList();
                        foreach (var h in hotels) tour.Hotels.Add(h);

                        var typeIds = _selectedTypes.Select(t => t.Id).ToList();
                        var types = db.Types.Where(t => typeIds.Contains(t.Id)).ToList();
                        foreach (var t in types) tour.Types.Add(t);

                        var serviceIds = _selectedServices.Select(s => s.Id).ToList();
                        var services = db.Services.Where(s => serviceIds.Contains(s.Id)).ToList();
                        foreach (var s in services) tour.Services.Add(s);

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

            MessageBox.Show(_tourId.HasValue ? "Тур обновлён." : "Тур создан.", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            FrameService.Frame.Navigate(new ToursPage());
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new ToursPage());
        }
    }
}
