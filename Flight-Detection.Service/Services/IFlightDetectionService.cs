using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flight_Detection.Entity.Models;

namespace Flight_Detection.Service.Services
{
    public interface IFlightDetectionService
    {
        List<FlightDetectionResult> GetRoutesByAgencyIdAndDuration(InputParameters inputParameters);
    }
}
