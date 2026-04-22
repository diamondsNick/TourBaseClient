using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using TourAgency2018.Services;
using TourAgency2018.Views.Windows;

namespace TourAgency2018.Views.Pages
{
    public partial class UserCabinetPage : Page
    {
        private static readonly Regex _alphanumeric = new Regex(@"^[A-Za-z0-9]+$");
        public UserCabinetPage()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        private void LoadUserInfo()
        {
            var user = SessionService.User;
            SurnameText.Text = user.Surname;
            NameText.Text = user.Name;
            PatronymicText.Text = string.IsNullOrWhiteSpace(user.Patronymic) ? "Не указано" : user.Patronymic;
            RoleText.Text = user.Role?.Name ?? "Не указано";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            var currentPassword = CurrentPasswordBox.Password;
            var newPassword = NewPasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            if (currentPassword.Length == 0 && newPassword.Length == 0 && confirmPassword.Length == 0)
            {
                var confirmExit = MessageBox.Show(
                    "Вы не изменили пароль. Выйти из аккаунта?",
                    "Выход",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmExit != MessageBoxResult.Yes) return;

                OpenLoginAndClose();
                return;
            }
            var correctUserPass = SessionService.User.Password;

            try
            {
                PasswodService.VerifyPasswordChange(newPassword, confirmPassword, correctUserPass, currentPassword);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        var user = db.Users.Find(SessionService.User.Id);
                        if (user == null) return;

                        user.Password = newPassword;
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

            MessageBox.Show("Пароль успешно изменён. Пожалуйста, войдите заново.", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);

            OpenLoginAndClose();
        }

        

        private void OpenLoginAndClose()
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Window.GetWindow(this)?.Close();
        }

        private void AlphanumericOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !_alphanumeric.IsMatch(e.Text);
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
