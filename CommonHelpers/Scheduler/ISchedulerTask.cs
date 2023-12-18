namespace HelpersCommon.Scheduler
{
    public interface ISchedulerTask
    {
        Task Run();
        DateTime IncreaseTime();

        public delegate void SetTimeDelegate(Type TaskType, DateTime newTime);
        static SetTimeDelegate SetTime;
    }
}
