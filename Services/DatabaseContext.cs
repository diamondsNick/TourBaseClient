using TourAgency2018.Models;

namespace TourAgency2018.Services
{
    public static class DatabaseContext
    {
        public static ToursBaseEntities GetEntities()
        {
            var entities = new ToursBaseEntities();
            entities.Configuration.AutoDetectChangesEnabled = false;
            return entities;
        }

    }
}
