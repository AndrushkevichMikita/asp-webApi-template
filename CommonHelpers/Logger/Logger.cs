using HelpersCommon.ExceptionHandler;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Text;

namespace HelpersCommon.Logger
{
    public enum LogLevel
    {
        Info,
        Debug,
        Error,
        CriticalError,
        TraceHttp
    }

    public class LogPair
    {
        public LogPair(DateTime date, string message)
        {
            Date = date;
            Message = message;
        }

        public DateTime Date { get; set; }
        public string Message { get; set; }
    }

    public class Logger : ILogger
    {
        public Logger(IConfiguration configuration)
        {
            if (configuration is not null)
                _settings ??= configuration.GetSection("FLogger").Get<LoggerSettings>();
        }

        private static Logger _instance;

        public static Logger Instance(IConfiguration configuration = null)
        {
            _instance ??= new Logger(configuration); // these case only for static usage (exmpl. Logger.Info())
            return _instance;
        }

        LoggerSettings _settings;

        public LoggerSettings Settings
        {
            get
            {
                _settings ??= new LoggerSettings();
                return _settings;
            }
            internal set { _settings = value; }
        }

        #region Inside

        public static List<LogPair> ErrorsInMemory { get; private set; } = new List<LogPair>();

        static readonly bool needZip = true;
        static readonly object lockSaveIntoMemory = new();
        void TrySaveIntoMemory(LogLevel level, DateTime dateTime, string message)
        {
            try
            {
                lock (lockSaveIntoMemory)
                {
                    if (Settings.ErrorsInMemoryCapacity > 0 && (level == LogLevel.Error || level == LogLevel.CriticalError))
                    {
                        if (ErrorsInMemory.Count >= Settings.ErrorsInMemoryCapacity)
                            ErrorsInMemory.RemoveAt(0);
                        ErrorsInMemory.Add(new LogPair(dateTime, message));
                    }
                }
            }
            catch { }
        }

        public void Log(LogLevel level, params string[] messages)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            if (Settings.AsyncEnable)
                Task.Run(() => { GoLog(level, threadId, messages); });
            else
                GoLog(level, threadId, messages);
        }

        void GoLog(LogLevel level, int threadId, params string[] messages)
        {
            lock (lockObj)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var str = new StringBuilder();
                    str.Append('[');
                    str.Append(now.ToString(Settings.DateTimeFormat));
                    str.Append(']');
                    str.Append(" Thread:");
                    str.Append(threadId.ToString("D3"));
                    str.Append(' ');
                    str.Append(level.ToString().ToUpper());
                    str.Append(" - ");

                    if (messages != null)
                    {
                        foreach (var message in messages)
                        {
                            if (message != null)
                            {
                                str.Append(message);
                                str.Append(' ');
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine(str.ToString());

                    if (level != LogLevel.TraceHttp || Settings.EnableHttpTraceLog)
                    {
                        str.Append(Environment.NewLine);
                        str.Append(Environment.NewLine);

                        var strResult = str.ToString();
                        TrySaveIntoMemory(level, now, strResult);

                        var filePath = Settings.FilePath;
                        if (!Settings.DisableLogInFile && !string.IsNullOrWhiteSpace(filePath))
                            SaveToFile(strResult, now, filePath);
                    }
                }
                catch (Exception ex)
                {
                    TrySaveIntoMemory(LogLevel.Error, DateTime.UtcNow, "LoggerSaveToFile error:" + Environment.NewLine + ex);
                }
            }
        }

        static readonly object lockObj = new();

        bool dirExists;
        int freeSpaceCnt = int.MaxValue - 1;
        void SaveToFile(string text, DateTime now, string filePath)
        {
            if (!dirExists)
            {
                var dir = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(dir);
                dirExists = true;
            }

            var isMoved = CheckArchive(now);
            if (++freeSpaceCnt > 100)
            {
                freeSpaceCnt = 0;
                CheckFreeSpace(filePath); // check freeSpace only every 100th log - for performance reason (so allow ~1MB accuracy)
            }
            try
            {
                File.AppendAllText(filePath, text);
            }
            catch (DirectoryNotFoundException)
            {
                dirExists = false;
                SaveToFile(text, now, filePath);
            }

            if (isMoved)
                File.SetCreationTime(filePath, now);
            lastCreation = now;
        }

        void CheckFreeSpace(string curFilePath)
        {
            var allowMB = Settings.FreeSpaceMB;
            var freeMB = new DriveInfo(Settings.FilePath).AvailableFreeSpace / 1024 / 1024;
            var missedMB = allowMB - freeMB;
            if (missedMB > 0)
            {
                // remove prev archives
                var arcFiles = GetArchiveFiles();
                for (int i = arcFiles.Length; i >= 0 && missedMB > 0; --i)
                {
                    missedMB -= arcFiles[i].Length / 1024 / 1024;
                    arcFiles[i].Delete();
                    System.Diagnostics.Debug.WriteLine("FLogger. Optimize FreeSpace. Removed: " + arcFiles[i].FullName);
                }
                if (missedMB > 0)
                {
                    File.Delete(curFilePath); // remove current file if it's huge and removing archive doesn't helped
                    System.Diagnostics.Debug.WriteLine("FLogger. Optimize FreeSpace. Removed: " + curFilePath);
                }
            }
        }

        DateTime lastCreation;
        /// <summary>
        /// Check if need archive current log-file and remove old ones; return true if file was archived
        /// </summary>
        bool CheckArchive(DateTime now)
        {

            bool isMoved = false;
            try
            {
                if (Settings.ArchiveFilePath == null)
                    return isMoved;

                if (lastCreation.Date == now.Date)
                    return isMoved;

                var dir = Path.GetDirectoryName(Settings.ArchiveFilePath);

                if (File.Exists(Settings.FilePath))
                {
                    if (lastCreation == DateTime.MinValue)
                    {
                        lastCreation = File.GetCreationTime(Settings.FilePath);
                        if (lastCreation.Date == now.Date)
                            return isMoved;
                    }
                    Directory.CreateDirectory(dir);
                    var destPath = Settings.ArchiveFilePath.Replace("{#}", now.AddDays(-1).ToString(Settings.ArchiveDateFormat));
                    if (File.Exists(destPath))
                        File.Delete(destPath);

                    if (needZip)
                    {
                        // zip file to reduce fileSize
                        var zipName = destPath + ".zip";
                        if (File.Exists(zipName))
                            File.Delete(zipName);
                        using (var zip = ZipFile.Open(zipName, ZipArchiveMode.Create))
                            zip.CreateEntryFromFile(Settings.FilePath, Path.GetFileName(Settings.FilePath));
                        File.Delete(Settings.FilePath);
                    }
                    else
                    {
                        File.Move(Settings.FilePath, destPath);
                    }
                    isMoved = true;
                }

                if (Settings.ArchiveMaxFiles != -1)
                {
                    var arcFiles = GetArchiveFiles();
                    for (int i = arcFiles.Length; i > Settings.ArchiveMaxFiles; --i)
                    {
                        arcFiles[i].Delete();
                        System.Diagnostics.Debug.WriteLine("FLogger. Removed old file: " + arcFiles[i].FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("FLogger. Got exception:");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            return isMoved;
        }

        FileInfo[] GetArchiveFiles()
        {
            var path = Settings.ArchiveFilePath.Replace("{#}", "*");
            var sortedFiles = new DirectoryInfo(Path.GetDirectoryName(path))
                                               .GetFiles(Path.GetFileName(needZip ? path + ".zip" : path))
                                               .OrderByDescending(f => f.LastWriteTime)
                                               .ToArray();
            return sortedFiles;
        }

        public (Stream, string) GetLogsByDate(DateTime logDate)
        {
            string logLocation;
            string outFileName;
            var date = logDate.ToString(Settings.ArchiveDateFormat);
            if (logDate == DateTime.UtcNow.Date)
            {
                logLocation = Settings.FilePath;
                outFileName = Path.GetFileName(Settings.ArchiveFilePath).Replace("{#}", date); // WARN: `today`-file returns unzipped
            }
            else
            {
                logLocation = Settings.ArchiveFilePath.Replace("{#}", date) + (needZip ? ".zip" : "");
                outFileName = Path.GetFileName(logLocation);
            }
            if (!File.Exists(logLocation))
                throw new MyApplicationException(ErrorStatus.NotFound, "Log file with provided date does not exist");

            var fs = File.OpenRead(logLocation);
            return (fs, outFileName);
        }
        #endregion

        #region Static
        public static void Info(string message)
        {
            Instance().Log(LogLevel.Info, message);
        }

        public static void Info(string request, string message)
        {
            Instance().Log(LogLevel.Info, request, message);
        }

        public static void Error(Exception ex)
        {
            Instance().Log(LogLevel.Error, ex.ToString());
        }

        public static void Error(string errorMsg, Exception ex)
        {
            Instance().Log(LogLevel.Error, errorMsg, Environment.NewLine, ex.ToString());
        }

        public static void ErrorCriticalSync(string errorMsg, Exception ex)
        {
            Instance().Log(LogLevel.CriticalError, errorMsg, Environment.NewLine, ex.ToString());
        }

        public static void ErrorSync(string errorMsg, Exception ex)
        {
            Instance().GoLog(LogLevel.Error, Thread.CurrentThread.ManagedThreadId, errorMsg, Environment.NewLine, ex.ToString());
        }

        public static void Error(string errorMsg)
        {
            Instance().Log(LogLevel.Error, errorMsg);
        }

        public static void Debug(string message)
        {
            Instance().Log(LogLevel.Debug, message);
        }
        #endregion

        #region  ILogger
        public void AddError(Exception ex)
        {
            Log(LogLevel.Error, ex.ToString());
        }

        public void AddError(string errorMsg, Exception ex)
        {
            Log(LogLevel.Error, errorMsg, Environment.NewLine, ex.ToString());
        }

        public void AddError(string errorMsg)
        {
            Log(LogLevel.Error, errorMsg);
        }

        public void AddInfo(string message)
        {
            Log(LogLevel.Info, message);
        }

        public void AddDebug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public void AddHttpTrace(string message)
        {
            Log(LogLevel.TraceHttp, message);
        }
        #endregion
    }
}
