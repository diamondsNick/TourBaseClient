using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TourAgency2018.Services;
using TourAgency2018.ViewModels;
using TourAgency2018.Views.Pages;

namespace TourAgency2018.Views.Windows
{
    /// <summary>
    /// Логика взаимодействия для MainAppWindow.xaml
    /// </summary>
    public partial class MainAppWindow : Window
    {
        private bool _allowRedactManager;

        public MainAppWindow()
        {
            InitializeComponent();

            ChangeButtonsVisibility();

            DataContext = this;

            FrameService.SetFrame(MainAppFrame);

            FrameService.Frame.Navigate(new Views.Pages.ToursPage());

            MainAppFrame.Navigated += OnFrameNavigated;
        }

        private void OnFrameNavigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            while (MainAppFrame.CanGoBack)
                MainAppFrame.RemoveBackEntry();
        }

        protected override void OnClosed(EventArgs e)
        {
            MainAppFrame.Navigated -= OnFrameNavigated;
            base.OnClosed(e);
        }

        private void ChangeButtonsVisibility()
        {
            var userRole = SessionService.User.Role.Name;

            switch (userRole)
            {
                case "Администратор":
                    _allowRedactManager = true;
                    ClientsButton.Visibility = Visibility.Collapsed;
                    ApplicationsButton.Visibility = Visibility.Collapsed;
                    return;
                case "Менеджер":
                    _allowRedactManager = false;
                    ServicesButton.Visibility = Visibility.Collapsed;
                    return;
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы точно хотите выйти? Все несохраненные изменения будут утеряны.", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void MainAppFrame_ContentRendered(object sender, EventArgs e)
        {
            bool allowShowingRedactingControls = false;

            if (MainAppFrame.Content is ToursPage && _allowRedactManager)
                allowShowingRedactingControls = true;

            if (MainAppFrame.Content is HotelsPage && _allowRedactManager)
                allowShowingRedactingControls = true;

            if (MainAppFrame.Content is ServicesPage && _allowRedactManager)
                allowShowingRedactingControls = true;

            if (MainAppFrame.Content is ClientsPage)
                allowShowingRedactingControls = true;

            if (MainAppFrame.Content is ApplicationsPage)
                allowShowingRedactingControls = true;

            AddButton.Visibility = allowShowingRedactingControls ? Visibility.Visible : Visibility.Hidden;
            RedactButton.Visibility = allowShowingRedactingControls ? Visibility.Visible : Visibility.Hidden;
            RemoveButton.Visibility = allowShowingRedactingControls ? Visibility.Visible : Visibility.Hidden;
        }

        private void ToursButton_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new ToursPage());
        }

        private void HotelsButton_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new HotelsPage());
        }

        private void ClientsButton_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new ClientsPage());
        }

        private void ApplicationsButton_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new ApplicationsPage());
        }

        private void ServicesButton_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new ServicesPage());
        }

        private void UserCabinet_Click(object sender, RoutedEventArgs e)
        {
            FrameService.Frame.Navigate(new UserCabinetPage());
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainAppFrame.Content is IAllowRedactPage page)
                page.Add();
        }

        private void RedactButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainAppFrame.Content is IAllowRedactPage page)
                page.Redact();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainAppFrame.Content is IAllowRedactPage page)
                page.Remove();
        }
    }
}
