using Play.Utils;
using System;
using System.Collections.Generic;

namespace Play.Airline
{
    public enum TaskType
    {
        Flight,
        Vacation,
        StandBy,
        Maintenance,
        Training,
        Duty
    }

    public class TaskItem
    {
        public TaskType Type { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? StartLocation { get; set; }
        public string? EndLocation { get; set; }
        public bool Cancelled { get; set; }
        public DateTime RestUntil { get; set; }
        public TimeSpan Duration => EndTime- StartTime;

        public TaskItem(TaskType type, 
            DateTime startTime, DateTime endTime, 
            string? startLocation = null, string? endLocation = null)
        {
            Type = type;
            StartTime = startTime;
            EndTime = endTime;
            RestUntil = endTime;
            StartLocation = startLocation;
            EndLocation = endLocation;
        }

        public (DateTime startDate, double[] hours) GetDailyWorkload()
        {
            return WorkloadUtils.GetDailyWorkload(StartTime, EndTime);
        }

        public static TaskItem CreateMonthTask(TaskType taskType, int month, int year)
        {
            return new TaskItem(taskType,
                new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(year + (month == 12 ? 1 : 0), month % 12 + 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        public override string ToString()
        {
            string loc = StartLocation != null && EndLocation != null
                ? " "+StartLocation + "-" + EndLocation + " "
                : "";

            return string.Format("{0} {2}{1}",
                Type,
                FormatString.GetDateString(StartTime, EndTime),
                loc);
        }

        
    }
}
