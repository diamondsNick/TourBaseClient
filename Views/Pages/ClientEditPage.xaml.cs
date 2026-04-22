using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TourAgency2018.Models;
using TourAgency2018.Services;

namespace TourAgency2018.Views.Pages
{
    public partial class ClientEditPage : Page
    {
        private static readonly Regex _alphanumeric = new Regex(@"^[A-Za-z0-9]+$");
        private readonly int? _clientId;

        public ClientEditPage(int? clientId = null)
        {
            InitializeComponent();
            _clientId = clientId;

            if (clientId.HasValue)
            {
                PageTitle.Text = "Редактирование клиента";
                PasswordHint.Visibility = Visibility.Visible;
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
                var user = db.Users.Find(id);
                if (user == null) return;

                SurnameBox.Text = user.Surname;
                NameBox.Text = user.Name;
                PatronymicBox.Text = user.Patronymic;
                LoginBox.Text = user.Login;
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

            var login = LoginBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(login) || login.Length < 4 || login.Length > 50)
            {
                MessageBox.Show("Логин обязателен и должен быть от 4 до 50 символов.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var password = PasswordBox.Password;
            if (!_clientId.HasValue && string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Укажите пароль для нового клиента.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(password) && (password.Length < 6 || password.Length > 50))
            {
                MessageBox.Show("Пароль должен быть от 6 до 50 символов.", "Внимание",
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

                        User user;

                        if (_clientId.HasValue)
                        {
                            user = db.Users.Find(_clientId.Value);
                            if (user == null) return;
                        }
                        else
                        {
                            var clientRole = db.Roles.FirstOrDefault(r => r.Name == "Клиент");
                            if (clientRole == null)
                            {
                                MessageBox.Show("Роль «Клиент» не найдена в базе данных.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            var loginExists = db.Users.Any(u => u.Login == login);
                            if (loginExists)
                            {
                                MessageBox.Show("Пользователь с таким логином уже существует.", "Внимание",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            user = new User { RoleId = clientRole.Id };
                            db.Users.Add(user);
                        }

                        user.Surname = SurnameBox.Text.Trim();
                        user.Name = NameBox.Text.Trim();
                        user.Patronymic = PatronymicBox.Text.Trim();
                        user.Login = login;

                        if (!string.IsNullOrEmpty(password))
                            user.Password = password;

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

        private void AlphanumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_alphanumeric.IsMatch(e.Text);
        }
    }
}
