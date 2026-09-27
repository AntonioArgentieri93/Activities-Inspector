using System;

namespace Activities_Inspector.Models
{
    public class RecentFolderEntry : Entry
    {
        /// <summary>null = data non documentata dalla sorgente (es. voci MRU non in prima posizione), mostrata come "N/D".</summary>
        public DateTime? ActionTime { get; set; }
        public string FileName { get; set; }
        public string DataSource { get; set; }
        public string FullPath { get; set; }

        public int SkippedShellItems { get; set; }

        /// <summary>
        /// Solo per voci RecentDocs: nome del .lnk in Recent\ a cui il valore fa riferimento
        /// (es. "Report.pdf.lnk"). Serve a unire la voce con quella del .lnk, che ha percorso e data precisi.
        /// </summary>
        internal string LinkedLnkName { get; set; }

        public RecentFolderEntry(DateTime? actionTime, string fileName, string dataSource, string fullPath)
        {
            ActionTime = actionTime;
            FileName = fileName;
            DataSource = dataSource;
            FullPath = fullPath;
        }
    }
}
