using Play.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Airline
{
    public class FlightDutyPeriod
    {
        public Aircraft Aircraft { get; }
        public List<RoundRobin> RoundRobins { get; }
        public DateTime StartTime => RoundRobins.FirstOrDefault()?.StartTime ?? DateTime.MaxValue;
        public DateTime EndTime => RoundRobins.LastOrDefault()?.EndTime ?? DateTime.MinValue;
        public TimeSpan Duration => EndTime- StartTime;
        public DateTime RestUntil { get; }
        public string? StartLocation => RoundRobins.FirstOrDefault()?.Flights.FirstOrDefault()?.StartLocation;

        public FlightDutyPeriod(List<RoundRobin> roundRobins)
        {
            RoundRobins = roundRobins;
            Aircraft = roundRobins.First().Aircraft;
            RestUntil = EndTime.AddHours(Math.Max(12, Duration.TotalHours));
        }

        public (DateTime startDate, double[] hours) GetDailyDutyWorkload()
        {
            return WorkloadUtils.GetDailyWorkload(StartTime, EndTime);
        }

        public bool ExceedsMaximum()
        {
            return Duration.TotalHours > 13;
        }

        public List<Flight> GetFlights()
        {
            List<Flight> flights = new();

            foreach (var rr in RoundRobins)
            {
                flights.AddRange(rr.Flights);
            }

            return flights;
        }

        public override string ToString()
        {
            return string.Format("Flights {0}: {1}",
                RoundRobins.Sum(rr => rr.Flights.Count),
                FormatString.GetDateString(StartTime, EndTime));
        }
    }
}