using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Airline
{
    public class VehicleType
    {
        public string Name { get; init; }
        public List<CrewMemberType> RequiredCrew { get; init; }

        public VehicleType(string name, List<CrewMemberType> requiredCrew) 
        { 
            Name = name;
            RequiredCrew = requiredCrew;
        }

        public int GetCrewMemberCountOfType(CrewMemberType type)
        {
            return RequiredCrew.Where(c => c == type).Count();
        }
    }
}
