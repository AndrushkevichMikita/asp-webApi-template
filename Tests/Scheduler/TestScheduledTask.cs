using HelpersCommon.Scheduler;

namespace Tests.Scheduler
{
    internal class TestScheduledTask : ISchedulerTask
    {
        static int execCnt = 0;

        public DateTime IncreaseTime() => execCnt != 0
                                       ? SchedulerExtension.TaskList.FirstOrDefault(x => x.TaskType == GetType()).CurrentTimeStart
                                       : DateTime.UtcNow;

        public async Task Run()
        {
            SchedulerExtension.SetTime(GetType(), DateTime.MaxValue);
            execCnt++;
        }
    }
}
