using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using TourAgency2018.Models;

namespace TourAgency2018.Services
{
    public static class SessionService
    {
        private static User _currentUser;
        public static User User => _currentUser;
        public static User SetUser(User user) => _currentUser = user;
    }
}
