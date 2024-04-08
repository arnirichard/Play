using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Airline
{
    public abstract class ScheduledResource
    {
        public List<TaskItem> Schedule { get; } = new();

        public TaskItem? GetTaskAtTime(DateTime dateTime)
        {
            TaskItem taskItem;

            for (int i = 0; i < Schedule.Count; i++)
            {
                taskItem = Schedule[i];

                if (dateTime >= taskItem.StartTime && dateTime <= taskItem.EndTime)
                {
                    return taskItem;
                }
            }

            return null;
        }

        public bool HasOverlappingTasks(DateTime start, DateTime end)
        {
            TaskItem taskItem;

            for (int i = 0; i < Schedule.Count; i++)
            {
                taskItem = Schedule[i];

                if (taskItem.EndTime <= start)
                {
                    // remove
                    if(taskItem.RestUntil > start)
                    {
                        return false;
                    }

                    continue;
                }

                if (taskItem.StartTime >= end)
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
