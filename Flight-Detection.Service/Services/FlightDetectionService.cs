using System;
using System.Collections.Generic;
using System.Linq;
using Flight_Detection.DataAccess;
using Flight_Detection.Entity.Enums;
using Flight_Detection.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace Flight_Detection.Service.Services
{
    public class FlightDetectionService : IFlightDetectionService
    {
        private readonly AppDbContext _context;

        public FlightDetectionService(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<FlightDetectionResult> GetRoutesByAgencyIdAndDuration(InputParameters inputParameters)
        {
            var routesQuery = _context.Routes
                .Join(_context.Subscriptions,
                    route => new { route.DestinationCityId, route.OriginCityId },
                    subscription => new { subscription.DestinationCityId, subscription.OriginCityId },
                    (route, subscription) => new { Route = route, Subscription = subscription })
                .Where(sr => sr.Subscription.AgencyId == inputParameters.AgencyId &&
                             (sr.Route.DepartureDate.Date >= inputParameters.StartDate.Date.AddDays(-7) &&
                              sr.Route.DepartureDate.Date <= inputParameters.EndDate.Date.AddDays(7)))
                .Select(subscription => subscription.Route.RouteId);

            var filteredFlights = _context.Flights
                .Where(flight => routesQuery.Any(routeId => routeId == flight.RouteId))
                .Include(flight => flight.Route)
                .OrderBy(flight => flight.DepartureTime)
                .ToList();

            return GetFlightDetectionResults(filteredFlights, inputParameters);
        }

        private static IEnumerable<FlightDetectionResult> GetFlightDetectionResults(
            IReadOnlyCollection<Flight> flights,
            InputParameters inputParameters)
        {
            var beforeTimeSpan = new TimeSpan(6, 23, 30, 0, 0);
            var afterTimeSpan = new TimeSpan(7, 0, 30, 0, 0);

            var groupedFlight = flights.GroupBy(flight => new
            {
                flight.AirlineId,
                flight.Route.OriginCityId,
                flight.Route.DestinationCityId,
                flight.DepartureTime.Date
            }).ToDictionary(
                d => (d.Key.AirlineId, d.Key.OriginCityId, d.Key.DestinationCityId, d.Key.Date),
                d => d.ToList());

            foreach (var flight in flights)
            {
                if (flight.DepartureTime < inputParameters.StartDate || flight.DepartureTime > inputParameters.EndDate)
                    continue;

                var originCityId = flight.Route.OriginCityId;
                var destinationCityId = flight.Route.DestinationCityId;

                groupedFlight.TryGetValue(
                    (flight.AirlineId, originCityId, destinationCityId, flight.DepartureTime.AddDays(-7).Date),
                    out var sevenDaysBeforeOfTheCurrentFlight);


                if (sevenDaysBeforeOfTheCurrentFlight is null || !sevenDaysBeforeOfTheCurrentFlight.Any(p =>
                        p.DepartureTime <= flight.DepartureTime.Subtract(beforeTimeSpan) &&
                        p.DepartureTime >= flight.DepartureTime.Subtract(afterTimeSpan)))
                {
                    yield return CreateFlightDetectionResultObject(flight, FlightStatusEnum.New);
                    continue;
                }

                groupedFlight.TryGetValue(
                    (flight.AirlineId, originCityId, destinationCityId, flight.DepartureTime.AddDays(7).Date),
                    out var sevenDaysAfterOfTheCurrentFlight);

                if (sevenDaysAfterOfTheCurrentFlight is null || !sevenDaysAfterOfTheCurrentFlight.Any(p =>
                        p.DepartureTime <= flight.DepartureTime.Add(afterTimeSpan) &&
                        p.DepartureTime >= flight.DepartureTime.Add(beforeTimeSpan)))
                {
                    yield return CreateFlightDetectionResultObject(flight, FlightStatusEnum.Discontinued);
                    continue;
                }

                yield return CreateFlightDetectionResultObject(flight, FlightStatusEnum.NotChanged);
            }
        }

        private static FlightDetectionResult CreateFlightDetectionResultObject(Flight flight, FlightStatusEnum status)
        {
            return new FlightDetectionResult
            {
                ArrivalTime = flight.ArrivalTime,
                AirlineId = flight.AirlineId,
                OriginCityId = flight.Route.OriginCityId,
                DepartureDate = flight.DepartureTime,
                DestinationCityId = flight.Route.DestinationCityId,
                FlightId = flight.FlightId,
                Status = status.ToString()
            };
        }
    }
}
