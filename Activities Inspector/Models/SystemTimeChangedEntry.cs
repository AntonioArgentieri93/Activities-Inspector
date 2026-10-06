using System;

namespace Activities_Inspector.Models
{
    public class SystemTimeChangedEntry : Entry
    {
        public string AccountName { get; set; }

        /// <summary>Ora dell'evento (ora locale). Date vere e non testo: il testo "gg/mm/aaaa" non si ordina cronologicamente.</summary>
        public DateTime TimeGenerated { get; set; }

        /// <summary>Orologio prima della modifica (ora locale).</summary>
        public DateTime OldTime { get; set; }

        /// <summary>Orologio dopo la modifica (ora locale).</summary>
        public DateTime NewTime { get; set; }

        public SystemTimeChangedEntry(string accountName, DateTime timeGenerated, DateTime oldTime, DateTime newTime)
        {
            AccountName = accountName;
            TimeGenerated = timeGenerated;
            OldTime = oldTime;
            NewTime = newTime;
        }
    }
}
