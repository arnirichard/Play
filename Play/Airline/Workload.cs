using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Airline
{
    public class Workload
    {
        public TaskType TaskType { get; }
        public int Days { get; }
        public double MaxHours { get; }
        public DateTime StartDate { get; internal set; } = DateTime.MinValue;
        public List<double> DailyWorkload { get; } = new();
        public List<double> AverageWorkload { get; } = new();

        public Workload(TaskType taskType, int days, double maxHours)
        {
            TaskType = taskType;
            Days = days;
            MaxHours = maxHours;
            AverageWorkload.AddRange(new double[days]);
        }

        public void ClearAll()
        {
            for(int i= 0; i< DailyWorkload.Count; i++)
            {
                DailyWorkload[i] = 0;
            }

            for (int i = 0; i < AverageWorkload.Count; i++)
            {
                AverageWorkload[i] = 0;
            }
            StartDate = DateTime.MinValue;
        }

        public bool ExceedsWorkload(DateTime startDate, double[] workload, double factor = 1)
        {
            if (StartDate == DateTime.MinValue || DailyWorkload.Count == 0)
            {
                StartDate = startDate;
            }
            else if (startDate < StartDate)
            {
                PrefixWorkloadLists((StartDate - startDate.Date).Days);
                StartDate = startDate;
            }

            int startIndex = (startDate.Date - StartDate.Date).Days;
            int requiredCount = startIndex + workload.Length;

            if (DailyWorkload.Count < requiredCount)
            {
                ExtendWorkloadLists(requiredCount);
            }

            double lastAv = startIndex < 0 ? 0 : AverageWorkload[startIndex];

            for (int i = 0; i < workload.Length; i++)
            {
                lastAv += workload[i]*factor;
                if(lastAv > MaxHours)
                {
                    return true;
                }
                if(i >= Days)
                {
                    lastAv -= workload[i - Days];
                }
            }

            return false;
        }

        public void AddWorkload(DateTime startDate, double[] workload, double factor = 1)
        {
            if (StartDate == DateTime.MinValue || DailyWorkload.Count == 0)
            {
                StartDate = startDate;
            }
            else if(startDate < StartDate)
            {
                PrefixWorkloadLists((StartDate - startDate.Date).Days);
                StartDate = startDate;
            }

            int startIndex = startDate.Date.Subtract(StartDate.Date).Days;
            int requiredCount = startIndex + workload.Length;
            
            if(DailyWorkload.Count < requiredCount)
            {
                ExtendWorkloadLists(requiredCount);
            }

            // Add workload
            for (int i = 0; i < workload.Length; i++)
            {
                DailyWorkload[startIndex + i] += workload[i]  * factor;
                for (int j = 0; j < Days; j++)
                {
                    AverageWorkload[startIndex + i + j] += workload[i]*factor;
                }
            }
        }

        void PrefixWorkloadLists(int days)
        {
            for(int i = 0; i < days;i++)
            {
                DailyWorkload.InsertRange(0, new double[days]);
                AverageWorkload.InsertRange(0, new double[days]);
            }
        }

        void ExtendWorkloadLists(int newSize)
        {
            int toAdd = newSize - DailyWorkload.Count;

            for (int j = 0; j < toAdd; j++)
            {
                DailyWorkload.Add(0);
            }

            toAdd = DailyWorkload.Count + Days - AverageWorkload.Count;

            for (int j = 0; j < toAdd; j++)
            {
                AverageWorkload.Add(0);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} Max hours {1} over {2} days",
                TaskType,
                MaxHours,
                Days);
        }
    }
}
