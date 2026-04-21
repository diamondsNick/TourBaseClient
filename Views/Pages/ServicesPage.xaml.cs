using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TourAgency2018.Models;
using TourAgency2018.Models.DTO;
using TourAgency2018.Services;
using TourAgency2018.ViewModels;
using TourAgency2018.Views.Windows;

namespace TourAgency2018.Views.Pages
{
    public partial class ServicesPage : Page, IAllowRedactPage
    {
        public ObservableCollection<ServiceDTO> ServicesList { get; set; } = new ObservableCollection<ServiceDTO>();
        public ServiceDTO SelectedService { get; set; }

        public ServicesPage()
        {
            InitializeComponent();
            DataContext = this;
            LoadServices();

            Unloaded += (s, e) =>
            {
                ServicesList.Clear();
                DataContext = null;
            };
        }

        private void LoadServices()
        {
            if (RecordCountText == null) return;

            ServicesList.Clear();

            var search = SearchTextBox.Text.Trim().ToLower();
            var isPlaceholder = search == "поиск...";

            using (var db = DatabaseContext.GetEntities())
            {
                var query = db.Services.AsQueryable();

                if (!isPlaceholder && search.Length > 0)
                    query = query.Where(s => s.Name.ToLower().Contains(search));

                var services = query
                    .OrderBy(s => s.Name)
                    .Select(s => new ServiceDTO
                    {
                        Id = s.Id,
                        Name = s.Name,
                        ToursCount = s.Tours.Count()
                    })
                    .ToList();

                foreach (var svc in services)
                    ServicesList.Add(svc);
            }

            RecordCountText.Text = $"Найдено: {ServicesList.Count}";
        }

        public void Add()
        {
            var dialog = new ServiceEditWindow { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var exists = db.Services.Any(s => s.Name.ToLower() == dialog.ServiceName.ToLower());
                        if (exists)
                        {
                            MessageBox.Show("Услуга с таким названием уже существует.", "Внимание",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        db.Services.Add(new Service { Name = dialog.ServiceName });
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

            LoadServices();
        }

        public void Redact()
        {
            if (SelectedService == null)
            {
                MessageBox.Show("Выберите услугу для редактирования.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new ServiceEditWindow(SelectedService.Name) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var exists = db.Services.Any(s =>
                            s.Name.ToLower() == dialog.ServiceName.ToLower() && s.Id != SelectedService.Id);
                        if (exists)
                        {
                            MessageBox.Show("Услуга с таким названием уже существует.", "Внимание",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var service = db.Services.Find(SelectedService.Id);
                        if (service == null) return;
                        service.Name = dialog.ServiceName;
                        db.Configuration.AutoDetectChangesEnabled = true;
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

            LoadServices();
        }

        public void Remove()
        {
            if (SelectedService == null)
            {
                MessageBox.Show("Выберите услугу для удаления.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedService.ToursCount > 0)
            {
                MessageBox.Show(
                    $"Невозможно удалить услугу «{SelectedService.Name}»: она используется в {SelectedService.ToursCount} тур(е/ах).",
                    "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить услугу «{SelectedService.Name}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var service = db.Services.Find(SelectedService.Id);
                        if (service == null) return;
                        db.Services.Remove(service);
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

            LoadServices();
            MessageBox.Show("Услуга успешно удалена.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск...";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadServices();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "Поиск...";
            SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            LoadServices();
        }
    }
}
