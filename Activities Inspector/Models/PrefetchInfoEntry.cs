using System;

namespace Activities_Inspector.Models
{
    public class PrefetchInfoEntry : Entry
    {
        public string ExecutableFileName { get; set; }
        public string SourceFileName { get; set; }
        public DateTime LastRunTime { get; set; }
        /// <summary>null se non tutte le esecuzioni sono registrate nel Prefetch (RunCount         public DateTime FirstRunTime { get; set; }gt; timestamp conservati): "N/D".</summary>
        public DateTime? FirstRunTime { get; set; }
        public int RunCount { get; set; }
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
