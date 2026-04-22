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
        private readonly bool _readOnly;
        private readonly string _userRole = SessionService.User?.Role?.Name;
        private List<Service> _tourServices = new List<Service>();

        public ApplicationEditPage(int? applicationId = null, bool readOnly = false)
        {
            InitializeComponent();
            _applicationId = applicationId;
            _readOnly = readOnly;

            LoadCatalogs();

            if (applicationId.HasValue)
            {
                PageTitle.Text = _readOnly ? "Просмотр заявки" : "Редактирование заявки";
                FillFields(applicationId.Value);
            }
            else
            {
                PageTitle.Text = "Новая заявка";
                ApplicationDatePicker.SelectedDate = DateTime.Today;
                StatusBox.SelectedIndex = 0;
            }

            if (_readOnly)
            {
                SaveButton.Visibility = Visibility.Collapsed;
                CancelButton.Content = "Назад";
            }
        }

        private void LoadCatalogs()
        {
            foreach (var s in Statuses)
                StatusBox.Items.Add(s);

            using (var db = DatabaseContext.GetEntities())
            {
                ClientBox.ItemsSource = db.Users
                    .Where(u => u.Role.Name == "Клиент")
                    .OrderBy(u => u.Surname).ThenBy(u => u.Name)
                    .ToList()
                    .Select(u => new ClientItem
                    {
                        Id = u.Id,
                        FullName = $"{u.Surname} {u.Name} {u.Patronymic}".Trim()
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

                ClientBox.IsEnabled = false;
                TourBox.IsEnabled = false;
                StatusBox.IsEnabled = false;
                ApplicationDatePicker.IsEnabled = false;
                ApplicationDatePicker.SelectedDate = app.Date ?? DateTime.Today;

                var selectedServiceIds = app.Services.Select(s => s.Id).ToHashSet();
                SetServicesForTour(app.Tour?.Services?.ToList(), selectedServiceIds);

                if (app.Status != "Новая" || _readOnly)
                    ServicesList.IsEnabled = false;

                ApplyVoucherSections(app);
            }
        }

        private void ApplyVoucherSections(TourApplication app)
        {
            var docStatus = app.DocumentGenerationStatus;
            var hasDoc = docStatus == "Запрошено" || docStatus == "Сгенерировано";

            if (_userRole == "Менеджер")
            {
                VoucherManagerSection.Visibility = Visibility.Visible;

                if (docStatus == "Сгенерировано")
                {
                    RequestVouchersButton.Content = "Сгенерировано";
                    RequestVouchersButton.IsEnabled = false;
                }
                else if (docStatus == "Запрошено")
                {
                    RequestVouchersButton.Content = "Запрошено";
                    RequestVouchersButton.IsEnabled = false;
                }
                else
                {
                    RequestVouchersButton.Content = "Запросить ваучеры";
                    RequestVouchersButton.IsEnabled = app.Status == "В обработке";
                }
            }
            else if (_userRole == "Клиент" && hasDoc)
            {
                VoucherClientSection.Visibility = Visibility.Visible;
            }
        }

        private void RequestVouchers_Click(object sender, RoutedEventArgs e)
        {
            if (!_applicationId.HasValue) return;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.Configuration.AutoDetectChangesEnabled = true;
                        var app = db.TourApplications.FirstOrDefault(a => a.Id == _applicationId.Value);
                        if (app == null) return;
                        app.DocumentGenerationStatus = "Запрошено";
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

            RequestVouchersButton.Content = "Запрошено";
            RequestVouchersButton.IsEnabled = false;
        }

        private void DownloadVoucher_Click(object sender, RoutedEventArgs e)
        {
            if (!_applicationId.HasValue) return;

            var btn = sender as System.Windows.Controls.Button;
            var label = btn?.Content?.ToString() ?? "";

            var defaultName = label.Replace(" ", "_") + ".pdf";

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = $"Сохранить «{label}»",
                FileName = defaultName,
                DefaultExt = ".pdf",
                Filter = "PDF файлы (*.pdf)|*.pdf"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                TourApplication app;
                using (var db = DatabaseContext.GetEntities())
                {
                    app = db.TourApplications
                        .Include(a => a.User)
                        .Include(a => a.Tour.Hotels.Select(h => h.Country))
                        .Include(a => a.Tour.Hotels.Select(h => h.MealType))
                        .FirstOrDefault(a => a.Id == _applicationId.Value);
                }

                if (app == null) return;

                switch (label)
                {
                    case "Ваучер на трансфер":
                        PdfGeneratorService.GenerateTransferVoucher(dlg.FileName, app);
                        break;
                    case "Ваучер на заселение":
                        PdfGeneratorService.GenerateHotelVoucher(dlg.FileName, app);
                        break;
                    case "Билет":
                        PdfGeneratorService.GenerateAirTicket(dlg.FileName, app);
                        break;
                    case "Страховой полис":
                        PdfGeneratorService.GenerateInsurancePolicy(dlg.FileName, app);
                        break;
                    case "Виза":
                        PdfGeneratorService.GenerateVisa(dlg.FileName, app);
                        break;
                }

                SetDocumentStatusGenerated();

                MessageBox.Show($"Файл сохранён:\n{dlg.FileName}", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetDocumentStatusGenerated()
        {
            if (!_applicationId.HasValue) return;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.Configuration.AutoDetectChangesEnabled = true;
                        var app = db.TourApplications.FirstOrDefault(a => a.Id == _applicationId.Value);
                        if (app == null) return;
                        app.DocumentGenerationStatus = "Сгенерировано";
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
