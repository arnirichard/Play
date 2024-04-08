using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Airline
{
    public class RoundRobin
    {
        public Aircraft Aircraft { get; }
        public List<Flight> Flights { get; }
        public DateTime StartTime => Flights.FirstOrDefault()?.StartTime ?? DateTime.MaxValue;
        public DateTime EndTime => Flights.LastOrDefault()?.EndTime ?? DateTime.MinValue;
        public TimeSpan Duration => EndTime - StartTime;

        public RoundRobin(Aircraft aircraft, List<Flight> flights)
        {
            Aircraft = aircraft;
            Flights = flights;
        }

        public override string ToString()
        {
            List<string> locations = new List<string>();

            string? lastLocation = "";

            foreach(var task in Flights)
            {
                if(task.StartLocation != null && task.StartLocation != lastLocation)
                {
                    locations.Add(task.StartLocation);
                }
                if (task.EndLocation != null)
                {
                    locations.Add(task.EndLocation);
                }
                lastLocation = task.EndLocation;
            }

            return string.Format("Flights {0}: {1}",
                Flights.Count,
                string.Join(", ", locations));
        }
    }
}
