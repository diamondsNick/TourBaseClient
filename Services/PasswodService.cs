using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace TourAgency2018.Services
{
    public static class PasswodService
    {
        private static readonly Regex _alphanumeric = new Regex(@"^[A-Za-z0-9]+$");

        public static bool VerifyPasswordChange(string newPassword,
            string confirmPassword,
            string currentPassword,
            string inputedCurrentUserPasswod)
        {
            if (currentPassword != inputedCurrentUserPasswod)
                throw new Exception("Текущий пароль введён неверно.");

            if (newPassword.Length < 6 || newPassword.Length > 50)
                throw new Exception("Новый пароль должен быть от 6 до 50 символов.");

            if (!_alphanumeric.IsMatch(newPassword))
                throw new Exception("Пароль может содержать только латинские буквы и цифры.");

            if (newPassword != confirmPassword)
                throw new Exception("Пароли не совпадают.");

            return true;
        }
    }
}
