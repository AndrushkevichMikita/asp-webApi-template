using HelpersCommon.PrimitivesExtensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace HelpersCommon.Logger
{
    public class LoggerSettings
    {
        public bool AsyncEnable { get; set; }
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

        string _filePath;
        public string FilePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_filePath))
                    _filePath = DefaultPath;

                return _filePath;
            }
            set
            {
                _filePath = value;
                if (!string.IsNullOrWhiteSpace(value))
                    _filePath = _filePath.ReplaceFirst("~/", AssemblyPath + "\\");
            }
        }

        public string ArchiveFilePath { get; set; }
        public string ArchiveDateFormat { get; set; } = "yyyyMMdd";
        public int ArchiveMaxFiles { get; set; } = 10;
        public int FreeSpaceMB { get; set; } = 100;
        public bool EnableHttpTraceLog { get; set; }
        public bool DisableLogInFile { get; set; }

        string assemblyPath;
        string AssemblyPath
        {
            get
            {
                if (assemblyPath == null)
                {
                    try
                    {
                        assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    }
                    catch (Exception ex)
                    {
                        assemblyPath = "";
                        Trace.WriteLine(ex);
                    }
                }
                return assemblyPath;
            }
        }

        public string DefaultPath { get => AssemblyPath + "\\_logfile.txt"; }
        public int ErrorsInMemoryCapacity { get; set; } = 100;
    }
}
