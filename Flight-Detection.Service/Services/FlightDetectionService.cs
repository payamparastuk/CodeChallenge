using System;
using System.Collections.Generic;
using System.Linq;
using Flight_Detection.DataAccess;
using Flight_Detection.Entity.Enums;
using Flight_Detection.Entity.Models;

namespace Flight_Detection.Service.Services
{
    public class FlightDetectionService : IFlightDetectionService
    {
        private readonly AppDbContext _context;

        public FlightDetectionService(AppDbContext context)
        {
            _context = context;
        }

        public List<FlightDetectionResult> GetRoutesByAgencyIdAndDuration(InputParameters inputParameters)
        {
            var routes = _context.Routes
                .Join(_context.Subscriptions,
                    route => new { route.DestinationCityId, route.OriginCityId },
                    subscription => new { subscription.DestinationCityId, subscription.OriginCityId },
                    (route, subscription) => new { Route = route, Subscription = subscription })
                .Where(sr => sr.Subscription.AgencyId == inputParameters.AgencyId &&
                             (sr.Route.DepartureDate.Date >= inputParameters.StartDate.Date &&
                              sr.Route.DepartureDate.Date <= inputParameters.EndDate.Date))
                .Select(subscription => subscription.Route).ToList();

            if (routes.Count == 0)
            {
                return new List<FlightDetectionResult>();
            }

            var routeIds = routes.Select(route => route.RouteId).ToList();

            var filteredFlights = _context.Flights
                .Where(flight => routeIds.Contains(flight.RouteId))
                .OrderBy(flight => flight.DepartureTime)
                .ToList();

            return GetFlightDetectionResults(filteredFlights, routes);
        }

        private static List<FlightDetectionResult> GetFlightDetectionResults(IEnumerable<Flight> flights,
            IReadOnlyCollection<Route> routes)
        {
            var beforeTimeSpan = new TimeSpan(6, 23, 30, 0, 0);
            var afterTimeSpan = new TimeSpan(7, 0, 30, 0, 0);

            var groupedFlight = flights.GroupBy(flight => new
            {
                flight.AirlineId,
                flight.DepartureTime.Date
            }).ToDictionary(d => (d.Key.AirlineId, d.Key.Date), d => d.ToList());

            var flightDetectionResults = flights.Select(flight =>
            {
                groupedFlight.TryGetValue((flight.AirlineId, flight.DepartureTime.AddDays(-7).Date),
                    out var sevenDaysBeforeOfTheCurrentFlight);
                groupedFlight.TryGetValue((flight.AirlineId, flight.DepartureTime.AddDays(7).Date),
                    out var sevenDaysAfterOfTheCurrentFlight);

                if (sevenDaysBeforeOfTheCurrentFlight is null || !sevenDaysBeforeOfTheCurrentFlight.Any(p =>
                        p.DepartureTime <= flight.DepartureTime.Subtract(beforeTimeSpan) &&
                        p.DepartureTime >= flight.DepartureTime.Subtract(afterTimeSpan)))
                {
                    return CreateFlightDetectionResultObject(routes, flight, FlightStatusEnum.New);
                }

                if (sevenDaysAfterOfTheCurrentFlight is null || !sevenDaysAfterOfTheCurrentFlight.Any(p =>
                        p.DepartureTime <= flight.DepartureTime.Add(afterTimeSpan) &&
                        p.DepartureTime >= flight.DepartureTime.Add(beforeTimeSpan)))
                {
                    return CreateFlightDetectionResultObject(routes, flight, FlightStatusEnum.Discontinued);
                }

                return CreateFlightDetectionResultObject(routes, flight, FlightStatusEnum.NotChanged);
            });

            return flightDetectionResults.OrderBy(f => f.FlightId).ToList();
        }

        private static FlightDetectionResult CreateFlightDetectionResultObject(
            IReadOnlyCollection<Route> routes, Flight flight, FlightStatusEnum status)
        {
            return new FlightDetectionResult
            {
                ArrivalTime = flight.ArrivalTime,
                AirlineId = flight.AirlineId,
                OriginCityId = routes.FirstOrDefault(p => p.RouteId == flight.RouteId)?.OriginCityId,
                DepartureDate = flight.DepartureTime,
                DestinationCityId = routes.FirstOrDefault(p => p.RouteId == flight.RouteId)?.DestinationCityId,
                FlightId = flight.FlightId,
                Status = status.ToString()
            };
        }
    }
}
