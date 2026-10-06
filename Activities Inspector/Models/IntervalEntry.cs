using System;

namespace Activities_Inspector.Models
{
    /// <summary>Come è terminato un intervallo di accensione.</summary>
    public enum IntervalEndKind
    {
        /// <summary>Nessuna fine registrata: il sistema risulta ancora acceso all'ultimo evento disponibile.</summary>
        InProgress,
        /// <summary>Arresto regolare (evento 6006).</summary>
        Shutdown,
        /// <summary>Sospensione o ibernazione (Kernel-Power 42).</summary>
        Sleep,
        /// <summary>
        /// Nessun evento di arresto: il sistema si è spento senza registrarlo (crash, blackout, spegnimento forzato).
        /// La fine è una STIMA: ora dell'ultimo evento registrato prima del successivo avvio.
        /// </summary>
        Unexpected
    }

    public class IntervalEntry
    {
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public bool StartedAfterCrash { get; set; }
        public IntervalEndKind EndKind { get; set; }

        public IntervalEntry(DateTime start, DateTime? end)
        {
            Start = start;
            End = end;
            EndKind = end.HasValue ? IntervalEndKind.Shutdown : IntervalEndKind.InProgress;
        }

        public IntervalEntry(DateTime? end)
        {
            End = end;
            EndKind = end.HasValue ? IntervalEndKind.Shutdown : IntervalEndKind.InProgress;
        }

        /// <summary>Descrizione della fine per interfaccia, CSV e report.</summary>
        public string EndKindText => ToText(EndKind);

        public static string ToText(IntervalEndKind kind)
        {
            switch (kind)
            {
                case IntervalEndKind.Shutdown: return "Spegnimento";
                case IntervalEndKind.Sleep: return "Sospensione";
                case IntervalEndKind.Unexpected: return "Anomalo (stimato)";
                default: return "In corso";
            }
        }
    }
}
