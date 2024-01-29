using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class InstallEntry : Entry
    {
        public string FileName { get; set; }
        public string DataSource { get; set; }
        public string FullPath { get; set; }
        public DateTime? InstallDate { get; set; }

        public InstallEntry(string fileName, string dataSource, string fullPath, DateTime? installDate)
        {
            FileName = fileName;
            DataSource = dataSource;
            FullPath = fullPath;
            InstallDate = installDate;
        }
    }
}
