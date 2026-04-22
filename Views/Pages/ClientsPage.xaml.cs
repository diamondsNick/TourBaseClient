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
    public partial class ClientsPage : Page, IAllowRedactPage
    {
        public ObservableCollection<ClientDTO> ClientsList { get; set; } = new ObservableCollection<ClientDTO>();
        public ClientDTO SelectedClient { get; set; }

        public ClientsPage()
        {
            InitializeComponent();
            DataContext = this;
            LoadClients();

            Unloaded += (s, e) =>
            {
                ClientsList.Clear();
                DataContext = null;
            };
        }

        private void LoadClients()
        {
            if (RecordCountText == null) return;

            ClientsList.Clear();

            var search = SearchTextBox.Text.Trim().ToLower();
            var isPlaceholder = search == "поиск по фио...";

            using (var db = DatabaseContext.GetEntities())
            {
                var query = db.Users
                    .Where(u => u.Role.Name == "Клиент")
                    .AsQueryable();

                if (!isPlaceholder && search.Length > 0)
                {
                    query = query.Where(u =>
                        u.Surname.ToLower().Contains(search) ||
                        u.Name.ToLower().Contains(search) ||
                        (u.Patronymic != null && u.Patronymic.ToLower().Contains(search)));
                }

                var clients = query
                    .OrderBy(u => u.Surname)
                    .ThenBy(u => u.Name)
                    .Select(u => new
                    {
                        u.Id,
                        u.Login,
                        u.Surname,
                        u.Name,
                        u.Patronymic,
                        ApplicationsCount = u.TourApplications.Count()
                    })
                    .ToList()
                    .Select(u => new ClientDTO
                    {
                        Id = u.Id,
                        Login = u.Login,
                        Surname = u.Surname,
                        Name = u.Name,
                        Patronymic = u.Patronymic,
                        ApplicationsCount = u.ApplicationsCount
                    })
                    .ToList();

                foreach (var client in clients)
                    ClientsList.Add(client);
            }

            RecordCountText.Text = $"Найдено: {ClientsList.Count}";
        }

        public void Add()
        {
            FrameService.Frame.Navigate(new ClientEditPage());
        }

        public void Redact()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Выберите клиента для редактирования.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FrameService.Frame.Navigate(new ClientEditPage(SelectedClient.Id));
        }

        public void Remove()
        {
            if (SelectedClient == null)
            {
                MessageBox.Show("Выберите клиента для удаления.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedClient.ApplicationsCount > 0)
            {
                MessageBox.Show(
                    $"Невозможно удалить клиента «{SelectedClient.FullName}»: у него есть {SelectedClient.ApplicationsCount} заявок(а).",
                    "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить клиента «{SelectedClient.FullName}»?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var db = DatabaseContext.GetEntities())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var user = db.Users.Find(SelectedClient.Id);
                        if (user == null) return;
                        db.Users.Remove(user);
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

            LoadClients();
            MessageBox.Show("Клиент успешно удалён.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск по ФИО...")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "Поиск по ФИО...";
                SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadClients();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "Поиск по ФИО...";
            SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            LoadClients();
        }
    }
}
