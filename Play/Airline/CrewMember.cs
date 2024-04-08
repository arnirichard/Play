using Play.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Play.Airline
{
    public enum CrewMemberType
    {
        Captain = 0,
        FirstOfficer = 1,
        SeniorCabinCrew = 2,
        CabinCrew = 3
    }

    public class CrewMember : ScheduledResource
    {
        public long Id { get; }
        public CrewMemberType Type { get; set; }
        public string HomeBase { get; }
        public List<FlightDutyPeriod> FlightDutyPeriods { get; } = new();
        public DateTime? StartDate => FlightDutyPeriods.FirstOrDefault()?.StartTime.Date;
        public List<Workload> Workloads { get; } = new();

        public CrewMember(long id, CrewMemberType type, string homeBase)
        {
            Id = id;
            Type = type;
            HomeBase = homeBase;
            Workloads.Add(new Workload(TaskType.Duty, 7, 60));
            Workloads.Add(new Workload(TaskType.Duty, 14, 110));
            Workloads.Add(new Workload(TaskType.Duty, 28, 190));
            Workloads.Add(new Workload(TaskType.Flight, 28, 100));
        }

        public void AddStandby(TaskItem standby)
        {
            (DateTime startDate, double[] hours) = WorkloadUtils.GetDailyWorkload(
                standby.StartTime, standby.EndTime);

            foreach (var wl in Workloads)
            {
                if (wl.TaskType == TaskType.Duty)
                {
                    wl.AddWorkload(startDate, hours, factor: 0.25);
                }
            }
            Schedule.Add(standby);
        }

        public TimeSpan AddFlightDuty(FlightDutyPeriod flightDuty)
        {
            TimeSpan result = FlightDutyPeriods.Count == 0
                ? TimeSpan.MaxValue
                : flightDuty.StartTime - FlightDutyPeriods.Last().RestUntil;

            FlightDutyPeriods.Add(flightDuty);

            (DateTime dutyStartTime, double[] dutyWorkLoad) = WorkloadUtils.GetDailyWorkload(
                flightDuty.RoundRobins.First().StartTime,
                flightDuty.RoundRobins.Last().EndTime);

            List<TaskItem> flights = new();
            foreach (var rr in flightDuty.RoundRobins)
            {
                flights.AddRange(rr.Flights.Where(t => t.Type == TaskType.Flight));
                Schedule.AddRange(rr.Flights);
            }
            (DateTime flightStartTime, double[] flightWorkload) = WorkloadUtils.GetDailyWorkload(flights);

            foreach (var wl in Workloads)
            {
                if(wl.TaskType == TaskType.Flight)
                {
                    wl.AddWorkload(flightStartTime, flightWorkload);
                }
                else if(wl.TaskType == TaskType.Duty)
                {
                    wl.AddWorkload(dutyStartTime, dutyWorkLoad);
                }
            }

            return result;
        }

        public TimeSpan? GetTimeLaxity(FlightDutyPeriod flightDuty)
        {
            // Minimum 12 hour rest after standby
            if(Schedule.Count > 0 && 
                Schedule.Last().Type == TaskType.StandBy &&
                Schedule.Last().RestUntil > flightDuty.StartTime)
            {
                return null;
            }

            (DateTime startTime, double[] workLoad) = WorkloadUtils.GetDailyWorkload(
                flightDuty.RoundRobins.First().StartTime,
                flightDuty.RoundRobins.Last().EndTime);

            if(Workloads.Any(w => w.TaskType == TaskType.Duty && w.ExceedsWorkload(startTime, workLoad)))
            {
                return null;
            }

            List<TaskItem> flights = new();
            foreach(var rr in flightDuty.RoundRobins)
            {
                flights.AddRange(rr.Flights.Where(t => t.Type == TaskType.Flight));
            }

            (DateTime flightStartTime, double[] flightWorkload) = WorkloadUtils.GetDailyWorkload(flights);

            if (Workloads.Any(w => 
                    w.TaskType == TaskType.Flight && w.ExceedsWorkload(flightStartTime, flightWorkload)))
            {
                return null;
            }

            return FlightDutyPeriods.Count == 0
                ? TimeSpan.MaxValue
                : flightDuty.StartTime - FlightDutyPeriods.Last().RestUntil;
        }

        public static CrewMember Create(long id, CrewMemberType type, 
            string homeBase, TaskItem? task = null)
        {
            CrewMember result = new (id, type, homeBase);

            if(task != null)
            {
                result.Schedule.Add(task);
            }

            return result;
        }

        public override string ToString()
        {
            return Type.ToString() +":"+ Id;
        }
    }
}
