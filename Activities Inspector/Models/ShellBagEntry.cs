using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class ShellBagEntry : Entry
    {
        public string AbsolutePath { get; set; }
        public DateTime LastRegistryWriteDate { get; set; }
        public string RegistryPath { get; set; }

        public ShellBagEntry(string absolutePath, DateTime lastRegistryWriteDate, string registryPath)
        {
            AbsolutePath = absolutePath;
            LastRegistryWriteDate = lastRegistryWriteDate;
            RegistryPath = registryPath;
        }
    }
}
