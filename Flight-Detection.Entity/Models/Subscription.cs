using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flight_Detection.Entity.Models
{
    public class Subscription
    {
        public int AgencyId { get; set; }
        public int OriginCityId { get; set; }
        public int DestinationCityId { get; set; }
    }
}
