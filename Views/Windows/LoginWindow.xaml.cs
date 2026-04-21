using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TourAgency2018.Services;

namespace TourAgency2018.Views.Windows
{

    public partial class LoginWindow : Window
    {
        private static readonly Regex _alphanumeric = new Regex(@"^[A-Za-z0-9]+$");

        public string Login { get; set; } = "";
        public string Password { get; set; } = "";
        public string AttentionMessage { get; set; } = "";
        public LoginWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoginTextBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Login = "admin";
            // Password = "admin123";
            Authentificate();
        }

        private void Authentificate()
        {
            // Валидация данных
            if (!ValidateData()) return;

            // Поиск пользователя
            var user = DatabaseContext.GetEntities().Users.FirstOrDefault(u => u.Login == Login);

            // Проверка существования пользователя
            if (user == null)
            {
                ErrorMessage.Text = "Пользователь не найден, перепроверьте данные";
                return;
            }

            if (user.Password != Password)
            {
                ErrorMessage.Text = "Неверный пароль, перепроверьте данные";
                return;
            }

            SessionService.SetUser(user);

            var mainWindow = new MainAppWindow();

            mainWindow.Show();

            this.Close();
        }

        private bool ValidateData()
        {
            ErrorMessage.Text = "";

            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage.Text = "Поля логина и пароля должны быть заполнены";
                return false;
            }

            var loginLength = Login.Length;
            var passwordLength = Password.Length;

            if (loginLength < 4 || loginLength >= 50)
            {
                ErrorMessage.Text = "Поле логина должно быть от 4 до 50 символов";
                return false;
            }

            if (passwordLength < 6 || passwordLength > 50)
            {
                ErrorMessage.Text = "Поле пароля должно быть от 6 до 50 символов";
                return false;
            }

            return true;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Authentificate();
        }

        private void AlphanumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_alphanumeric.IsMatch(e.Text);
        }
    }
}
