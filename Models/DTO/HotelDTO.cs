namespace TourAgency2018.Models.DTO
{
    public class HotelDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public string Stars { get; set; }
        public string MealType { get; set; }
        public byte[] Image { get; set; }
    }
}
