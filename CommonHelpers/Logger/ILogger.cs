namespace HelpersCommon.Logger
{
    public interface ILogger
    {
        void AddError(Exception ex);
        void AddInfo(string message);
        void AddDebug(string message);
        void AddHttpTrace(string message);
        void AddError(string errorMsg);
        void AddError(string errorMsg, Exception ex);
        (Stream, string) GetLogsByDate(DateTime logDate);
    }
}
