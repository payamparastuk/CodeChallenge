using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flight_Detection.Entity.Models
{
    public class InputParameters
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AgencyId { get; set; }
    }
}
