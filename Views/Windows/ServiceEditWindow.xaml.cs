using System.Windows;

namespace TourAgency2018.Views.Windows
{
    public partial class ServiceEditWindow : Window
    {
        public string ServiceName => NameTextBox.Text.Trim();

        public ServiceEditWindow(string existingName = null)
        {
            InitializeComponent();
            if (existingName != null)
            {
                Title = "Редактирование услуги";
                NameTextBox.Text = existingName;
            }
            else
            {
                Title = "Новая услуга";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название услуги.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
