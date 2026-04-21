using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TourAgency2018.Models.DTO
{
    public class TourDTO
    {
        public int Id { get; set; }
        public string Tickets { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Types { get; set; }
        public byte[] Image { get; set; }
        public string Price { get; set; }
        public string Status { get; set; }
        public string Countries { get; set; }
        public string Hotels { get; set; }
    }
}
