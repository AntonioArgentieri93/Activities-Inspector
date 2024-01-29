using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class RecentFolderEntry : Entry
    {
        public DateTime ActionTime { get; set; }
        public string FileName { get; set; }
        public string DataSource { get; set; }
        public string FullPath { get; set; }

        public RecentFolderEntry(DateTime actionTime, string fileName, string dataSource, string fullPath)
        {
            ActionTime = actionTime;
            FileName = fileName;
            DataSource = dataSource;
            FullPath = fullPath;
        }
    }
}
