using System;

namespace TourAgency2018.Models.DTO
{
    public class ClientDTO
    {
        public int Id { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string Patronymic { get; set; }
        public string FullName => $"{Surname} {Name} {Patronymic}".Trim();
        public string DateOfBirth { get; set; }
        public string PassportSeries { get; set; }
        public string PassportNumber { get; set; }
        public int ApplicationsCount { get; set; }
    }
}
