using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TourAgency2018.Models;
using TourAgency2018.Services;

namespace TourAgency2018.Views.Pages
{
    public partial class ApplicationEditPage : Page
    {
        private static readonly string[] Statuses = { "Новая", "В обработке", "Выполнена", "Закрыта" };

        private readonly int? _applicationId;
        private List<Service> _tourServices = new List<Service>();

        public ApplicationEditPage(int? applicationId = null)
        {
            InitializeComponent();
            _applicationId = applicationId;

            LoadCatalogs();

            if (applicationId.HasValue)
            {
                PageTitle.Text = "Редактирование заявки";
                FillFields(applicationId.Value);
            }
            else
            {
                PageTitle.Text = "Новая заявка";
                ApplicationDatePicker.SelectedDate = DateTime.Today;
                StatusBox.SelectedIndex = 0;
            }
        }

        private void LoadCatalogs()
        {
            foreach (var s in Statuses)
                StatusBox.Items.Add(s);

            using (var db = DatabaseContext.GetEntities())
            {
                ClientBox.ItemsSource = db.Clients
                    .OrderBy(c => c.Surname).ThenBy(c => c.Name)
                    .ToList()
                    .Select(c => new ClientItem
                    {
                        Id = c.Id,
                        FullName = $"{c.Surname} {c.Name} {c.Patronymic}".Trim()
                    })
                    .ToList();

                TourBox.ItemsSource = db.Tours
                    .Where(t => t.IsActual)
                    .OrderBy(t => t.Name)
                    .ToList();
            }
        }

        private void FillFields(int id)
        {
            using (var db = DatabaseContext.GetEntities())
            {
                var app = db.TourApplications
                    .Include(a => a.Tour.Services)
                    .Include(a => a.Services)
                    .FirstOrDefault(a => a.Id == id);

                if (app == null) return;

                ClientBox.SelectedItem = (ClientBox.ItemsSource as List<ClientItem>)
                    ?.FirstOrDefault(c => c.Id == app.ClientId);

                TourBox.SelectedItem = (TourBox.ItemsSource as List<Tour>)
                    ?.FirstOrDefault(t => t.Id == app.TourId);

                StatusBox.SelectedItem = app.Status;
                ApplicationDatePicker.SelectedDate = app.Date ?? DateTime.Today;

                var selectedServiceIds = app.Services.Select(s => s.Id).ToHashSet();
                SetServicesForTour(app.Tour?.Services?.ToList(), selectedServiceIds);
            }
        }

        private void TourBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TourBox.SelectedItem is Tour tour)
            {
                using (var db = DatabaseContext.GetEntities())
                {
                    var services = db.Tours
                        .Include(t => t.Services)
                        .FirstOrDefault(t => t.Id == tour.Id)
                        ?.Services.ToList();

                    SetServicesForTour(services, null);
                }
            }
            else
            {
                NoServicesPlaceholder.Text = "Сначала выберите тур";
                NoServicesPlaceholder.Visibility = Visibility.Visible;
                ServicesList.ItemsSource = null;
                _tourServices.Clear();
            }
        }

        private void SetServicesForTour(IList<Service> services, ISet<int> selectedIds)
        {
            if (services != null && services.Any())
            {
                _tourServices = services.ToList();
                NoServicesPlaceholder.Visibility = Visibility.Collapsed;

                var items = services.Select(s => new ServiceCheckItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsChecked = selectedIds?.Contains(s.Id) ?? false
                }).ToList();

                ServicesList.ItemsSource = items;
            }
            else
            {
                _tourServices.Clear();
                NoServicesPlaceholder.Text = "У выбранного тура нет доп. услуг";
                NoServicesPlaceholder.Visibility = Visibility.Visible;
                ServicesList.ItemsSource = null;
            }
        }

        private List<int> GetCheckedServiceIds()
        {
            return (ServicesList.ItemsSource as List<ServiceCheckItem>)
                ?.Where(s => s.IsChecked)
                .Select(s => s.Id)
                .ToList() ?? new List<int>();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ClientBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TourBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите тур.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StatusBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус заявки.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ApplicationDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату заявки.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var clientId = ((ClientItem)ClientBox.SelectedItem).Id;
            var tour = (Tour)TourBox.SelectedItem;
            var status = (string)StatusBox.SelectedItem;
            var date = ApplicationDatePicker.SelectedDate.Value;
            var checkedIds = GetCheckedServiceIds();

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.Configuration.AutoDetectChangesEnabled = true;

                        TourApplication app;

                        if (_applicationId.HasValue)
                        {
                            app = db.TourApplications
                                .Include(a => a.Services)
                                .FirstOrDefault(a => a.Id == _applicationId.Value);
                            if (app == null) return;
                            app.Services.Clear();
                        }
                        else
                        {
                            app = new TourApplication();
                            db.TourApplications.Add(app);
                        }

                        app.ClientId = clientId;
                        app.TourId = tour.Id;
                        app.Status = status;
                        app.Date = date;

                        if (checkedIds.Any())
                        {
                            var services = db.Services.Where(s => checkedIds.Contains(s.Id)).ToList();
                            foreach (var s in services)
                                app.Services.Add(s);
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

            MessageBox.Show(_applicationId.HasValue ? "Заявка обновлена." : "Заявка создана.", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            FrameService.Frame.Navigate(new ApplicationsPage());
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new ApplicationsPage());
        }

        private class ClientItem
        {
            public int Id { get; set; }
            public string FullName { get; set; }
        }

        private class ServiceCheckItem : INotifyPropertyChanged
        {
            public int Id { get; set; }
            public string Name { get; set; }

            private bool _isChecked;
            public bool IsChecked
            {
                get => _isChecked;
                set
                {
                    _isChecked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
