using System;

namespace Activities_Inspector.Models
{
    public class ShellBagEntry : Entry
    {
        public string AbsolutePath { get; set; }

        /// <summary>
        /// Ultima interazione con la cartella (ora locale): LastWriteTime della chiave genitore, solo se la cartella è
        /// in posizione 0 nel MRUListEx (come "Last Interacted" di ShellBags Explorer). null = non documentata ("N/D").
        /// </summary>
        public DateTime? LastInteracted { get; set; }

        /// <summary>LastWriteTime (ora locale) della chiave BagMRU della cartella: cambia quando cambiano le sue sottocartelle.</summary>
        public DateTime? LastRegistryWriteDate { get; set; }

        /// <summary>Date della cartella registrate nello shell item (file system al momento della registrazione), ora locale.</summary>
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public DateTime? AccessedOn { get; set; }

        public string RegistryPath { get; set; }

        public ShellBagEntry(string absolutePath, DateTime? lastRegistryWriteDate, string registryPath)
        {
            AbsolutePath = absolutePath;
            LastRegistryWriteDate = lastRegistryWriteDate;
            RegistryPath = registryPath;
        }
    }
}
