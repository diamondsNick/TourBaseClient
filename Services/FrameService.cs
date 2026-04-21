using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TourAgency2018.Services
{
    public class FrameService
    {
        private static Frame _frame { get; set; }
        public static Frame Frame => _frame;
        public static Frame SetFrame (Frame frame) => _frame = frame;
    }
}
