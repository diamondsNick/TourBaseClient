using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TourAgency2018.Models;
using TourAgency2018.Services;

namespace TourAgency2018.Views.Pages
{
    public partial class ReviewEditPage : Page
    {
        private readonly int _hotelId;
        private int? _existingCommentId;

        public ReviewEditPage(int hotelId)
        {
            InitializeComponent();
            _hotelId = hotelId;
            LoadHotelAndComment();
        }

        private void LoadHotelAndComment()
        {
            var userId = SessionService.User.Id;

            using (var db = DatabaseContext.GetEntities())
            {
                var hotel = db.Hotels
                    .Include(h => h.Country)
                    .Include(h => h.MealType)
                    .Include(h => h.HotelImages)
                    .FirstOrDefault(h => h.Id == _hotelId);

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
                HotelCountry.Text = $"Страна: {hotel.Country?.Name ?? "Не указано"}";
                HotelStars.Text = $"Звёзды: {hotel.CountOfStars} ★";
                HotelMealType.Text = $"Тип питания: {hotel.MealType?.Name ?? "Не указано"}";

                var existing = db.HotelComments
                    .FirstOrDefault(c => c.HotelId == _hotelId && c.ClientId == userId);

                if (existing != null)
                {
                    _existingCommentId = existing.Id;
                    CommentBox.Text = existing.Text;
                    PageTitle.Text = "Редактировать отзыв";
                }
                else
                {
                    PageTitle.Text = "Оставить отзыв";
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var text = CommentBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Введите текст отзыва.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var userId = SessionService.User.Id;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.Configuration.AutoDetectChangesEnabled = true;

                        if (_existingCommentId.HasValue)
                        {
                            var comment = db.HotelComments.Find(_existingCommentId.Value);
                            if (comment != null)
                            {
                                comment.Text = text;
                                comment.CreationDate = DateTime.Today;
                            }
                        }
                        else
                        {
                            var comment = new HotelComment
                            {
                                HotelId = _hotelId,
                                ClientId = userId,
                                Text = text,
                                CreationDate = DateTime.Today
                            };
                            db.HotelComments.Add(comment);
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

            MessageBox.Show("Отзыв сохранён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            FrameService.Frame.Navigate(new ReviewsPage());
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new ReviewsPage());
        }
    }
}
