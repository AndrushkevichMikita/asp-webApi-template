using Microsoft.Extensions.DependencyInjection;

namespace HelpersCommon.Scheduler
{
    public static class SchedulerExtension
    {
        public static List<SchedulerItem> TaskList = new();

        public static void AddScheduler(this IServiceCollection services, List<SchedulerItem> tasks)
        {
            ISchedulerTask.SetTime = SetTime;
            TaskList.AddRange(tasks);
            TaskList.ForEach(t => services.AddScoped(t.TaskType));
        }

        public static void SetTime(Type TaskType, DateTime newTime)
        {
            var task = TaskList.First(x => x.TaskType == TaskType);
            task.CurrentTimeStart = new DateTime(Math.Min(task.CurrentTimeStart.Ticks, newTime.Ticks));
        }

        public static void StopExec(Type TaskType, bool stop)
        {
            var task = TaskList.First(x => x.TaskType == TaskType);
            task.StopExec = stop;
        }
    }

    public class SchedulerItem
    {
        public Type TaskType { get; set; }
        public DateTime CurrentTimeStart { get; set; }
        public bool IsBusy { get; set; }
        public bool StopExec { get; set; }
    }
}
