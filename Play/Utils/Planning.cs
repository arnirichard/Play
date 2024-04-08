using DynamicData;
using Play.Airline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Utils
{
    internal static class Planning
    {
        public static void Plan(List<Aircraft> aircrafts, 
            List<CrewMember> crewMembers, 
            string homeBase)
        {
            RoundRobin[][] roundRobins = GetRoundRobins(aircrafts, homeBase);
            int[] indexes = new int[aircrafts.Count];
            int i;
            int counter = 0;
            CrewMemberType[] crewMemberTypes = Enum.GetValues<CrewMemberType>();
            TaskItem?[] standbys = new TaskItem[crewMemberTypes.Length];

            while(true)
            {
                RoundRobin? roundRobin = null;
                int acIndex = 0;

                for (int aci = 0; aci < indexes.Length; aci++)
                {
                    i = indexes[aci];
                    if(i < roundRobins[aci].Length && 
                        (roundRobin == null || roundRobins[aci][i].StartTime < roundRobin.StartTime))
                    {
                        roundRobin = roundRobins[aci][i];
                        acIndex = aci;
                    }
                }

                if(roundRobin == null) 
                {
                    break; // All planned
                }

                i = indexes[acIndex];
                // Planning this RoundRobin
                FlightDutyPeriod fdp = new FlightDutyPeriod(new List<RoundRobin> { roundRobin });

                List<CrewMember>? plannedCrew = PlanFDP(fdp, crewMembers, true);

                if(plannedCrew != null)
                {
                    while(i+1 < roundRobins[acIndex].Length)
                    {
                        i++;
                        List<RoundRobin> rbList = fdp.RoundRobins.ToList();
                        rbList.Add(roundRobins[acIndex][i]);
                        FlightDutyPeriod fdpMulti = new FlightDutyPeriod(rbList);
                        if(fdpMulti.ExceedsMaximum())
                        {
                            break;
                        }
                        List<CrewMember>? plannedCrewMulti = PlanFDP(fdp, crewMembers, false);
                        if(plannedCrewMulti == null)
                        {
                            break;
                        }
                        fdp = fdpMulti;
                        plannedCrew = plannedCrewMulti;
                    }

                    foreach(var c in plannedCrew)
                    {
                        c.AddFlightDuty(fdp);
                    }

                    CrewMemberType crewMemberType;
                    TaskItem? standby;
                    for(int k = 0; k < crewMemberTypes.Length; k++)
                    {
                        crewMemberType = crewMemberTypes[k];
                        standby = standbys[k];
                        if(standby != null && WorkloadUtils.StandbyCoversFTP(standby, fdp))
                        {
                            DateTime suggestedEnd = fdp.StartTime.AddHours(1);

                            if(fdp.EndTime < suggestedEnd)
                            {
                                suggestedEnd = fdp.EndTime;
                            }
                            if (standby.EndTime < suggestedEnd)
                            {
                                standby.EndTime = suggestedEnd;
                            }
                            if (fdp.EndTime > standby.RestUntil)
                            {
                                standby.RestUntil = fdp.EndTime;
                            }
                        }
                        else
                        {
                            standbys[k] = standby = new TaskItem(TaskType.StandBy, fdp.StartTime, fdp.StartTime.AddHours(4));
                            standby.RestUntil = standby.EndTime.AddHours(12);
                            if (fdp.EndTime > standby.RestUntil)
                            {
                                standby.RestUntil = fdp.EndTime;
                            }
                            CrewMember crew = PlanStanby(crewMembers, crewMemberType, fdp);
                            crew.AddStandby(standby);
                        }
                        if(fdp.RestUntil > standby.RestUntil)
                        {
                            standby.RestUntil = fdp.RestUntil;
                        }
                    }
                }
                else // Cancel the flight, there is not available crew
                {
                    roundRobin.Flights.ForEach(t => t.Cancelled = true);
                }

                indexes[acIndex] += fdp.RoundRobins.Count;
                counter += fdp.RoundRobins.Count;
            }
        }

        static CrewMember PlanStanby(List<CrewMember> crewMembers, CrewMemberType crewMemberType, FlightDutyPeriod fdp)
        {
            CrewMember? result = null;
            double maxLaxity = -1;
            List<CrewMember> crewOfType = crewMembers.Where(c => c.Type == crewMemberType).ToList();

            foreach (var crew in crewOfType)
            {
                if(crew.Type != crewMemberType)
                {
                    continue;
                }                

                if (crew.HasOverlappingTasks(fdp.StartTime, fdp.EndTime))
                {
                    continue;
                }

                if (crew.FlightDutyPeriods.LastOrDefault()?.StartTime >= fdp.StartTime)
                {
                    //continue;
                }

                TimeSpan? laxity = crew.GetTimeLaxity(fdp);

                if(laxity != null && crew.Schedule.LastOrDefault()?.Type == TaskType.StandBy)
                {
                    laxity = new TimeSpan(); // laxity.Value - TimeSpan.FromDays(2);
                }

                if (laxity?.TotalHours >= maxLaxity)
                {
                    result = crew;
                    maxLaxity = laxity.Value.TotalHours;
                }
            }

            if(result == null)
            {
                if(crewMemberType == CrewMemberType.Captain)
                {

                }

                result = CrewMember.Create(crewOfType.Count + 1,
                        crewMemberType,
                        fdp.StartLocation ?? "KEF");
                crewMembers.Add(result);
            }

            return result;
        }

        static List<CrewMember>? PlanFDP(FlightDutyPeriod fdp, List<CrewMember> crewMembers, bool hire)
        {
            List<(CrewMember c, TimeSpan laxity)> laxities = new();
            
            foreach (var crew in crewMembers)
            {
                if(crew.HasOverlappingTasks(fdp.StartTime, fdp.EndTime))
                {
                    continue;
                }

                TimeSpan? laxity = crew.GetTimeLaxity(fdp);

                if (laxity?.TotalHours >= 0)
                {
                    if (crew.Id == 12 && crew.Type == CrewMemberType.Captain)
                    {

                    }

                    laxities.Add((crew, laxity.Value));
                }
            }

            var groups = fdp.Aircraft.Type.RequiredCrew.GroupBy(c => c);
            var availableGroups = laxities.GroupBy(k => k.c.Type).ToDictionary(c => c.Key,c => c.ToList()); ;
            List <CrewMember> result = new();
            int required;
            foreach (var group in groups)
            {
                required = group.Count(); 
                List<(CrewMember c, TimeSpan laxity)>? availableCrew;
                if(!availableGroups.TryGetValue(group.Key, out availableCrew))
                {
                    availableCrew = new();
                }

                if (required > availableCrew.Count)
                {
                    if (!hire)
                    {
                        return null;
                    }
                    else
                    {
                        int currentCount = crewMembers.Where(c => c.Type == group.Key).Count();
                        
                        for (int i = 1; i <= required- availableCrew.Count; i++)
                        {
                            CrewMember newCrew = CrewMember.Create(currentCount + i,
                                    group.Key,
                                    fdp.StartLocation ?? "KEF");
                            crewMembers.Add(newCrew);
                            result.Add(newCrew);
                        }
                        required = availableCrew.Count;
                    }
                }

                result.AddRange(
                    availableCrew.OrderByDescending(g => g.laxity).Select(t => t.c).ToList().GetRange(0, required)
                );
            }

            return result;
        }

        static RoundRobin[][] GetRoundRobins(List<Aircraft> aircrafts,
            string homeBase)
        {
            RoundRobin[][] result = new RoundRobin[aircrafts.Count][];

            List<(TaskItem task, Aircraft ac)> cancelFlights = new();

            Aircraft aircraft;

            for (int i = 0; i < aircrafts.Count; i++)
            {
                aircraft = aircrafts[i];
                List<RoundRobin> roundRobins = new();

                List<Flight> tasks = new();
                string? lastLocation = aircraft.Schedule.FirstOrDefault()?.StartLocation;

                foreach (var task in aircraft.Schedule)
                {
                    if (task is Flight flight)
                    {
                        if (flight.StartLocation != lastLocation)
                        {
                            cancelFlights.Add((flight, aircraft));
                            continue;
                        }

                        if (tasks.Count == 0 && flight.StartLocation != homeBase)
                        {
                            cancelFlights.Add((flight, aircraft));
                            continue;
                        }

                        tasks.Add(flight);

                        if (flight.EndLocation == tasks[0].StartLocation)
                        {
                            roundRobins.Add(new RoundRobin(aircraft, tasks));
                            tasks = new() { };
                        }

                        lastLocation = flight.EndLocation;
                    }
                }

                if (tasks.Count > 0)
                {
                    roundRobins.Add(new RoundRobin(aircraft, tasks));
                }

                result[i] = roundRobins.ToArray();
            }

            return result;
        }
    }
}
