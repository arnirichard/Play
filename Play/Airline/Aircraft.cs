using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Airline
{
    public class Aircraft : ScheduledResource
    {
        public string VehicleRegistration { get; init; }
        public VehicleType Type { get; init; }
        
        public Aircraft(string vehicleRegistration, VehicleType type) 
        { 
            VehicleRegistration = vehicleRegistration;
            Type = type;
        }
    }
}
