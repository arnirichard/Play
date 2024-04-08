using DynamicData;
using Microsoft.VisualBasic;
using Play.Airline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Utils
{
    public static class WorkloadUtils
    {
        public static bool StandbyCoversFTP(TaskItem standby, FlightDutyPeriod fdp)
        {
            // 1. The maximum duration of standby is 16 hours;
            if(fdp.StartTime - standby.StartTime <= TimeSpan.FromHours(16))
            {
                return true;
            }

            // 3. The Company ensures that the combination of standby and FDP do not lead to more than 18 hours awake
            // time;

            if(fdp.EndTime-standby.StartTime <= TimeSpan.FromHours(18))
            {
                return true;
            }

            return false;
        }

        public static (DateTime startDate, double[] hours) GetDailyWorkload(List<TaskItem> tasks)
        {
            if(tasks.Count == 0)
            {
                return (DateTime.MinValue, new double[0]);
            }

            DateTime startDate = tasks.Select(t => t.StartTime).Min();
            DateTime endDate = tasks.Select(t => t.EndTime).Max();

            double[] hours = new double[(endDate.Date-startDate.Date).Days+1];
            DateTime t0, t1;

            foreach (var task in tasks)
            {
                DateTime date = task.StartTime.Date;

                while(true)
                {
                    if (date > task.EndTime.Date)
                    {
                        break;
                    }
                    t0 = date > task.StartTime ? date : task.StartTime;
                    t1 = date.AddDays(1) < task.EndTime ? date.AddDays(1) : task.EndTime;
                    hours[(date-startDate.Date).Days] += ((t1 - t0).TotalHours);
                    date = date.AddDays(1);
                }
            }

            double h = hours.Sum();
            double h2 = tasks.Sum(t => t.Duration.TotalHours);

            return (startDate.Date, hours);
        }

        public static (DateTime startDate, double[] hours) GetDailyWorkload(
            DateTime startTime, DateTime endTime)
        {
            DateTime date = startTime.Date;
            List<double> hours = new();
            DateTime t0, t1;

            while (true)
            {
                if (date > endTime.Date)
                {
                    break;
                }
                t0 = date > startTime ? date : startTime;
                date = date.AddDays(1);
                t1 = date < endTime ? date : endTime;
                hours.Add((t1 - t0).TotalHours);
            }

            return (date, hours.ToArray());
        }
    }
}
