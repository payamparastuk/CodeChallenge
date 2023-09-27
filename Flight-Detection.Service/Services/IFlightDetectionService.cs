using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flight_Detection.Entity.ViewModels;

namespace Flight_Detection.Service.Services
{
    public interface IFlightDetectionService
    {
        IEnumerable<FlightDetectionResult> GetRoutesByAgencyIdAndDuration(InputParameters inputParameters);
    }
}
