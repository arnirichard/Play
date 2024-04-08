using Play.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Airline
{
    public class Flight : TaskItem
    {
        public string Code { get; set; }
        public List<CrewMember> Crew { get; } = new();

        public Flight(DateTime startTime, DateTime endTime,
                    string startLocation, string endLocation, string code)
            : base(TaskType.Flight, startTime, endTime, startLocation, endLocation)
        {
            Code = code;
        }

        public static Flight CreateFlight(DateTime startTime, DateTime endTime,
            string startLocation, string endLocation, string code)
        {
            return new Flight(startTime, endTime, startLocation, endLocation, code);
        }

        public override string ToString()
        {
            string loc = StartLocation != null && EndLocation != null
                ? " " + StartLocation + "-" + EndLocation + " "
                : "";

            return string.Format("{0} {3} {2}{1}",
                Type,
                FormatString.GetDateString(StartTime, EndTime),
                loc,
                Code);
        }
    }
}
