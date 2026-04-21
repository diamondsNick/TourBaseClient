using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TourAgency2018.Models;
using TourAgency2018.Services;

namespace TourAgency2018.Views.Pages
{
    public partial class HotelEditPage : Page
    {
        private readonly int? _hotelId;
        private byte[] _imageBytes;

        public HotelEditPage(Hotel hotel = null)
        {
            InitializeComponent();

            LoadCatalogs();

            if (hotel != null && hotel.Id != 0)
            {
                _hotelId = hotel.Id;
                PageTitle.Text = "Редактирование отеля";
                FillFields(hotel);
            }
            else
            {
                PageTitle.Text = "Создание отеля";
            }
        }

        private void LoadCatalogs()
        {
            using (var db = DatabaseContext.GetEntities())
            {
                CountryBox.ItemsSource = db.Countries.OrderBy(c => c.Name).ToList();
                MealTypeBox.ItemsSource = db.MealTypes.OrderBy(m => m.Name).ToList();
            }

            for (int i = 1; i <= 5; i++)
                StarsBox.Items.Add(i);
            StarsBox.SelectedIndex = 0;
        }

        private void FillFields(Hotel hotel)
        {
            using (var db = DatabaseContext.GetEntities())
            {
                var full = db.Hotels
                    .Include(h => h.Country)
                    .Include(h => h.MealType)
                    .Include(h => h.HotelImages)
                    .FirstOrDefault(h => h.Id == hotel.Id);

                if (full == null) return;

                NameBox.Text = full.Name;
                StarsBox.SelectedItem = full.CountOfStars;

                CountryBox.SelectedItem = (CountryBox.ItemsSource as System.Collections.Generic.List<Country>)
                    ?.FirstOrDefault(c => c.Code == full.CountryCode);

                MealTypeBox.SelectedItem = (MealTypeBox.ItemsSource as System.Collections.Generic.List<MealType>)
                    ?.FirstOrDefault(m => m.Code == full.MealTypeCode);

                var firstImage = full.HotelImages.FirstOrDefault();
                if (firstImage != null)
                {
                    _imageBytes = firstImage.ImageSource;
                    SetPreviewImage(_imageBytes);
                }
            }
        }

        private void SetPreviewImage(byte[] bytes)
        {
            var bmp = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
            }
            PreviewImage.Source = bmp;
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите название отеля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CountryBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите страну.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedCountry = (Country)CountryBox.SelectedItem;
            var selectedMealType = MealTypeBox.SelectedItem as MealType;
            var stars = (int)StarsBox.SelectedItem;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.Configuration.AutoDetectChangesEnabled = true;

                        Hotel hotel;

                        if (_hotelId.HasValue)
                        {
                            hotel = db.Hotels
                                .Include(h => h.HotelImages)
                                .FirstOrDefault(h => h.Id == _hotelId.Value);

                            if (hotel == null) return;

                            hotel.HotelImages.Clear();
                        }
                        else
                        {
                            hotel = new Hotel();
                            db.Hotels.Add(hotel);
                        }

                        hotel.Name = NameBox.Text.Trim();
                        hotel.CountOfStars = stars;
                        hotel.CountryCode = selectedCountry.Code;
                        hotel.MealTypeCode = selectedMealType?.Code;

                        if (_imageBytes != null)
                        {
                            db.HotelImages.Add(new HotelImage
                            {
                                Hotel = hotel,
                                ImageSource = _imageBytes
                            });
                        }

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

            MessageBox.Show(_hotelId.HasValue ? "Отель обновлён." : "Отель создан.", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            FrameService.Frame.Navigate(new HotelsPage());
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new HotelsPage());
        }
    }
}
