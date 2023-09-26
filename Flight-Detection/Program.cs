﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using Flight_Detection.DataAccess;
using Flight_Detection.Entity.Models;
using Flight_Detection.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Flight_Detection.Presentation
{
    class Program
    {
        private static IFlightDetectionService _flightDetectionService;

        static void Main(string[] args)
        {
            try
            {
                RegisterServices();

                var inputParameters = GetDetectionParams(args);

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Console.WriteLine("Please wait...");

                var flightDetectionResults = _flightDetectionService.GetRoutesByAgencyIdAndDuration(inputParameters);

                if (flightDetectionResults.Count == 0)
                {
                    Console.WriteLine("No flight is detected!");
                    return;
                }

                stopwatch.Stop();

                CreateResultFile(flightDetectionResults, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void CreateResultFile(
            List<FlightDetectionResult> lstFlightDetectionResults, long executionMetric)
        {
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string resultFile = Path.Combine(directory, "results.csv");

            using (var writer = new StreamWriter(resultFile))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(lstFlightDetectionResults);
            }

            Console.WriteLine("The file results.csv with {0} records has been created on your desktop!",
                lstFlightDetectionResults.Count);
            Console.WriteLine("The execution metrics is {0} ms!", executionMetric);

            Console.WriteLine("Please press a key to continue ... !");
            Console.ReadKey();
        }

        private static InputParameters GetDetectionParams(string[] args)
        {
            var input = new InputParameters();
            input.StartDate = GetStartDate(args.ElementAtOrDefault(0));
            input.EndDate = GetEndDate(args.ElementAtOrDefault(1));
            input.AgencyId = GetAgencyId(args.ElementAtOrDefault(2));

            return input;

            #region Local Functions

            DateTime GetStartDate(string startDateInput)
            {
                while (string.IsNullOrEmpty(startDateInput))
                {
                    Console.WriteLine("Please Enter start date (in yyyy-mm-dd format):");
                    startDateInput = Console.ReadLine();
                }

                return DateTime.SpecifyKind(DateTime.Parse(startDateInput), DateTimeKind.Utc);
            }

            DateTime GetEndDate(string endDateInput)
            {
                DateTime? endDate = null;
                while (endDate == null)
                {
                    if (string.IsNullOrEmpty(endDateInput))
                    {
                        Console.WriteLine("Please Enter end date (in yyyy-mm-dd format):");
                        endDateInput = Console.ReadLine();
                    }

                    if (!string.IsNullOrEmpty(endDateInput))
                    {
                        endDate = DateTime.SpecifyKind(DateTime.Parse(endDateInput), DateTimeKind.Utc);

                        if (endDate <= input.StartDate)
                        {
                            Console.WriteLine("End date should not be less than or equal to start date!");
                        }
                    }
                }

                return endDate.Value;
            }

            int GetAgencyId(string agencyIdInput)
            {
                while (string.IsNullOrEmpty(agencyIdInput))
                {
                    Console.WriteLine("Please Enter agency id:");
                    agencyIdInput = Console.ReadLine();
                }

                return int.Parse(agencyIdInput);
            }

            #endregion
        }

        private static void RegisterServices()
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFlightDetectionService, FlightDetectionService>()
                .AddDbContext<AppDbContext>()
                .BuildServiceProvider();

            _flightDetectionService = serviceProvider.GetRequiredService<IFlightDetectionService>();
        }
    }
}
