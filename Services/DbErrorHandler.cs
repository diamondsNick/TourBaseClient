using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Windows;

namespace TourAgency2018.Services
{
    public static class DbErrorHandler
    {
        public static void Handle(Exception ex)
        {
            if (IsConnectionError(ex))
            {
                MessageBox.Show("Потеря подключения к базе данных. Проверьте соединение и повторите попытку.",
                    "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ex is DbUpdateException)
            {
                MessageBox.Show("Параметры введены некорректно и не могут быть сохранены. Проверьте введённые данные и повторите попытку.",
                    "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Произошла непредвиденная ошибка:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static bool IsConnectionError(Exception ex)
        {
            var current = ex;
            while (current != null)
            {
                if (current is SqlException sqlEx)
                {
                    // Коды: сеть недоступна, сервер не найден, истекло время ожидания
                    return sqlEx.Number == -2 || sqlEx.Number == 2 || sqlEx.Number == 53
                        || sqlEx.Number == 1231 || sqlEx.Number == 10060;
                }
                current = current.InnerException;
            }
            return false;
        }
    }
}
