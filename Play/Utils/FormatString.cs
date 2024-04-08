using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Utils
{
    public static class FormatString
    {
        public static string GetDateString(DateTime StartTime, DateTime EndTime)
        {
            return StartTime.Date == EndTime.Date
                ? StartTime.Date.ToString("dd/MM") + " " + StartTime.ToString("HH:mm") + "-" + EndTime.ToString("HH:mm")
                : StartTime.Date.ToString("dd/MM") + " " + StartTime.ToString("HH:mm") + "-" +
                    EndTime.Date.ToString("dd/MM") + " " + EndTime.ToString("HH:mm");
        }
    }
}
