using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;

using System.Windows;
using System.Windows.Controls;
using TourAgency2018.Models.DTO;
using TourAgency2018.Services;
using TourAgency2018.ViewModels;

namespace TourAgency2018.Views.Pages
{
    public partial class ApplicationsPage : Page, IAllowRedactPage
    {
        private static readonly string[] Statuses = { "Все", "Новая", "В обработке", "Выполнена", "Закрыта" };

        public ObservableCollection<ApplicationDTO> ApplicationsList { get; set; } = new ObservableCollection<ApplicationDTO>();
        public ApplicationDTO SelectedApplication { get; set; }

        public ApplicationsPage()
        {
            InitializeComponent();
            DataContext = this;

            foreach (var s in Statuses)
                StatusFilterBox.Items.Add(s);
            StatusFilterBox.SelectedIndex = 0;

            LoadApplications();

            Unloaded += (s, e) =>
            {
                ApplicationsList.Clear();
                DataContext = null;
            };
        }

        private void LoadApplications()
        {
            if (RecordCountText == null) return;

            ApplicationsList.Clear();

            var search = SearchTextBox.Text.Trim().ToLower();
            var isPlaceholder = search == "поиск по клиенту или туру...";
            var selectedStatus = StatusFilterBox.SelectedItem as string;

            using (var db = DatabaseContext.GetEntities())
            {
                var query = db.TourApplications
                    .Include(a => a.Client)
                    .Include(a => a.Tour)
                    .AsQueryable();

                if (selectedStatus != null && selectedStatus != "Все")
                    query = query.Where(a => a.Status == selectedStatus);

                var list = query.ToList();

                foreach (var a in list)
                {
                    var fullName = $"{a.Client?.Surname} {a.Client?.Name} {a.Client?.Patronymic}".Trim();

                    if (!isPlaceholder && search.Length > 0)
                    {
                        if (!fullName.ToLower().Contains(search) &&
                            !(a.Tour?.Name ?? "").ToLower().Contains(search))
                            continue;
                    }

                    ApplicationsList.Add(new ApplicationDTO
                    {
                        Id = a.Id,
                        ClientFullName = fullName,
                        TourName = a.Tour?.Name ?? "Не указано",
                        Status = a.Status ?? "Не указано"
                    });
                }
            }

            RecordCountText.Text = $"Найдено: {ApplicationsList.Count}";
        }

        public void Add()
        {
            FrameService.Frame.Navigate(new ApplicationEditPage());
        }

        public void Redact()
        {
            if (SelectedApplication == null)
            {
                MessageBox.Show("Выберите заявку для редактирования.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FrameService.Frame.Navigate(new ApplicationEditPage(SelectedApplication.Id));
        }

        public void Remove()
        {
            if (SelectedApplication == null)
            {
                MessageBox.Show("Выберите заявку для удаления.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedApplication.Status != "Новая")
            {
                MessageBox.Show("Удалить можно только заявку со статусом «Новая».", "Удаление невозможно",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Удалить заявку №{SelectedApplication.Id}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var app = db.TourApplications
                            .Include(a => a.Services)
                            .FirstOrDefault(a => a.Id == SelectedApplication.Id);
                        if (app == null) return;

                        app.Services.Clear();

                        db.TourApplications.Remove(app);
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

            LoadApplications();
            MessageBox.Show("Заявка успешно удалена.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск по клиенту или туру...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск по клиенту или туру...";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => LoadApplications();
        private void StatusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadApplications();

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "Поиск по клиенту или туру...";
            SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            StatusFilterBox.SelectedIndex = 0;
        }
    }
}
