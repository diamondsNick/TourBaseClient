using System;
using System.Data.Entity;
using System.Windows;
using System.Windows.Controls;
using TourAgency2018.Models;
using TourAgency2018.Services;

namespace TourAgency2018.Views.Pages
{
    public partial class ClientEditPage : Page
    {
        private readonly int? _clientId;

        public ClientEditPage(int? clientId = null)
        {
            InitializeComponent();
            _clientId = clientId;

            if (clientId.HasValue)
            {
                PageTitle.Text = "Редактирование клиента";
                LoadClient(clientId.Value);
            }
            else
            {
                PageTitle.Text = "Новый клиент";
            }
        }

        private void LoadClient(int id)
        {
            using (var db = DatabaseContext.GetEntities())
            {
                var client = db.Clients.Find(id);
                if (client == null) return;

                SurnameBox.Text = client.Surname;
                NameBox.Text = client.Name;
                PatronymicBox.Text = client.Patronymic;
                DateOfBirthPicker.SelectedDate = client.DateOfBirth;
                PassportSeriesBox.Text = client.PassportSeries;
                PassportNumberBox.Text = client.PassportNumber;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SurnameBox.Text) ||
                string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Заполните обязательные поля: Фамилия и Имя.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DateOfBirthPicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Укажите дату рождения.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DateOfBirthPicker.SelectedDate.Value > DateTime.Today)
            {
                MessageBox.Show("Дата рождения не может быть в будущем.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var series = PassportSeriesBox.Text.Trim();
            var number = PassportNumberBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(series) || series.Length > 6)
            {
                MessageBox.Show("Серия паспорта обязательна и не должна превышать 6 символов.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(number) || number.Length > 4)
            {
                MessageBox.Show("Номер паспорта обязателен и не должен превышать 4 символа.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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

                        Client client;

                        if (_clientId.HasValue)
                        {
                            client = db.Clients.Find(_clientId.Value);
                            if (client == null) return;
                        }
                        else
                        {
                            client = new Client();
                            db.Clients.Add(client);
                        }

                        client.Surname = SurnameBox.Text.Trim();
                        client.Name = NameBox.Text.Trim();
                        client.Patronymic = PatronymicBox.Text.Trim();
                        client.DateOfBirth = DateOfBirthPicker.SelectedDate.Value;
                        client.PassportSeries = series;
                        client.PassportNumber = number;

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

            MessageBox.Show("Данные клиента сохранены.", "Сохранение",
                MessageBoxButton.OK, MessageBoxImage.Information);
            FrameService.Frame.Navigate(new ClientsPage());
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new ClientsPage());
        }
    }
}
