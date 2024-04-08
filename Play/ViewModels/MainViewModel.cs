using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia;
using Play.Airline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia.Media;
using System.Text.RegularExpressions;
using System.Globalization;
using Avalonia.Controls.Documents;
using System.Linq;
using System.Collections.ObjectModel;
using Play.Utils;
using DynamicData;

namespace Play.ViewModels;

public class MainViewModel : ViewModelBase
{
    const string DefaultHomeBase = "KEF";
    public DateTime earliestDate = DateTime.MaxValue, latestDate = DateTime.MinValue;
    public VehicleType A320 { get; } = GetA320();
    public VehicleType A321 { get; } = GetA321();
    public Dictionary<string, VehicleType> VehicleTypes { get; } = new();
    public Dictionary<string, Aircraft> Aircrafts { get; } = new();
    public List<TaskItem> Flights { get; } = new();
    public ObservableCollection<CrewMember> CrewMembers { get; } = new();
    public DateTime EarliestDate => earliestDate;
    public DateTime LatestDate => latestDate;

    public MainViewModel()
    {
        VehicleTypes.Add(A320.Name, A320);
        VehicleTypes.Add(A321.Name, A321);

        AddAircraft(new Aircraft("TFPPA", A320));
        AddAircraft(new Aircraft("TFPPB", A320));
        AddAircraft(new Aircraft("TFPPC", A320));
        AddAircraft(new Aircraft("TFPPD", A320));
        AddAircraft(new Aircraft("TFPPE", A320));
        AddAircraft(new Aircraft("TFPPF", A320));
        AddAircraft(new Aircraft("TFAEW", A321));
        AddAircraft(new Aircraft("TFPLA", A321));
        AddAircraft(new Aircraft("TFPLB", A321));
        AddAircraft(new Aircraft("TFPLC", A321));

        List<Flight> removed = ReadFlights();
        
        AddVacationCrewMembers(CrewMemberType.Captain, 5, 2, DefaultHomeBase);
        AddVacationCrewMembers(CrewMemberType.FirstOfficer, 5, 2, DefaultHomeBase);
        AddVacationCrewMembers(CrewMemberType.SeniorCabinCrew, 5, 0, DefaultHomeBase);
        AddVacationCrewMembers(CrewMemberType.CabinCrew, 25, 0, DefaultHomeBase);

        var allCrew = CrewMembers.ToList();

        Planning.Plan(Aircrafts.Values.ToList(), allCrew, DefaultHomeBase);

        foreach (var c in allCrew)
        {
            c.FlightDutyPeriods.Clear();
            c.Workloads.ForEach(w => w.ClearAll());
            c.Schedule.RemoveAll(t => t.Type != TaskType.Vacation &&
                t.Type != TaskType.Training);
        }

        Planning.Plan(Aircrafts.Values.ToList(), allCrew, DefaultHomeBase);

        allCrew = allCrew
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Id)
            .ToList();

        CrewMembers.Clear();
        CrewMembers.AddRange(allCrew);
    }

    void AddAircraft(Aircraft aircraft)
    {
        Aircrafts[aircraft.VehicleRegistration] = aircraft;
    }

    void AddVacationCrewMembers(CrewMemberType type, 
        int vacationsPerMonths, int trainingsPerMonth, string homeBase)
    {
        int counter = 1;
        for (int m = 0; m < 3; m++)
        {
            for (int i = 1; i <= vacationsPerMonths; i++)
            {
                CrewMembers.Add(CrewMember.Create(
                        counter++,
                        type, 
                        homeBase,
                        TaskItem.CreateMonthTask(TaskType.Vacation, 5+m, 2023)
                    )
                );
            }
            for (int i = 1; i <= trainingsPerMonth; i++)
            {
                CrewMembers.Add(CrewMember.Create(
                        counter++,
                        type,
                        homeBase,
                        TaskItem.CreateMonthTask(TaskType.Training, 5 + m, 2023)
                    )
                );
            }
        }
    }

    List<Flight> ReadFlights()
    {
        var stream = AssetLoader.Open(new Uri("avares://Play/Assets/Flights.txt"));

        using (StreamReader reader = new StreamReader(stream))
        {
            // Read the entire content of the stream into a string.
            string? line;
            string pattern = @"[\t\s ]+";
            string dateFormat = "dd-MM-yy HH:mm";
            Aircraft? aircraft;
            DateTime startTime, endTime;
            Flight task;
            List<Flight> removed = new();

            // Read and process lines one by one until the end of the file is reached.
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = Regex.Split(line, pattern).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                if (parts.Length == 7)
                {
                    string flightCode = parts[1];
                    string startLocation = parts[3];
                    string endLocation = parts[4];

                    if (Aircrafts.TryGetValue(parts[6], out aircraft) &&
                        DateTime.TryParseExact(parts[0] + " " + parts[2], dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out startTime) &&
                        DateTime.TryParseExact(parts[0] + " " + parts[5], dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out endTime))
                    {
                        if (endTime < startTime)
                        {
                            endTime = endTime.AddDays(1);
                        }
                        task = Flight.CreateFlight(startTime, endTime, startLocation, endLocation, flightCode);

                        if(aircraft.Schedule.LastOrDefault()?.EndTime > startTime)
                        {
                            removed.Add(task);
                            continue;
                        }

                        Flights.Add(task);
                        aircraft.Schedule.Add(task);

                        if(startTime.Date < earliestDate)
                        {
                            earliestDate = startTime.Date;
                        }
                        if (endTime.Date.AddDays(1) > latestDate)
                        {
                            latestDate = endTime.Date.AddDays(1);
                        }
                    }
                    else
                    {

                    }
                }
                else
                {

                }
            }

            return removed;
        }
    }

    private static VehicleType GetA320()
    {
        return new VehicleType("A320", new List<CrewMemberType>()
            {
                CrewMemberType.Captain,
                CrewMemberType.FirstOfficer,
                CrewMemberType.SeniorCabinCrew,
                CrewMemberType.CabinCrew,
                CrewMemberType.CabinCrew,
                CrewMemberType.CabinCrew
            }); ;
    }

    private static VehicleType GetA321()
    {
        return new VehicleType("A321", new List<CrewMemberType>()
            {
                CrewMemberType.Captain,
                CrewMemberType.FirstOfficer,
                CrewMemberType.SeniorCabinCrew,
                CrewMemberType.CabinCrew,
                CrewMemberType.CabinCrew,
                CrewMemberType.CabinCrew,
                CrewMemberType.CabinCrew
            }); ;
    }

    private static VehicleType GetA340()
    {
        return new VehicleType("A340", new List<CrewMemberType>()
            {
                CrewMemberType.Captain,
                CrewMemberType.FirstOfficer,
                CrewMemberType.SeniorCabinCrew,
                CrewMemberType.CabinCrew,
                CrewMemberType.CabinCrew,
                CrewMemberType.CabinCrew,
                CrewMemberType.CabinCrew,
                CrewMemberType.CabinCrew,
                CrewMemberType.CabinCrew,
            }); ;
    }
}
