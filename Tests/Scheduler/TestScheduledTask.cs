using HelpersCommon.Scheduler;

namespace Tests.Scheduler
{
    internal class TestScheduledTask : ISchedulerTask
    {
        static int execCnt = 0;

        public DateTime IncreaseTime() => execCnt != 0
                                       ? SchedulerExtension.TaskList.First(x => x.TaskType == GetType()).CurrentTimeStart
                                       : DateTime.UtcNow;

        public Task Run()
        {
            SchedulerExtension.SetTime(GetType(), DateTime.MaxValue);
            execCnt++;
            return Task.CompletedTask;
        }
    }
}
