using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class PrefetchInfoEntry : Entry
    {
        public string ExecutableFileName { get; set; }
        public string SourceFileName { get; set; }
        public DateTime LastRunTime { get; set; }
        public string Extension { get; set; }

        public PrefetchInfoEntry(string executableFileName, string sourceFileName, DateTime lastRunTime, string extension)
        {
            ExecutableFileName = executableFileName;
            SourceFileName = sourceFileName;
            LastRunTime = lastRunTime;
            Extension = extension;
        }
    }
}
