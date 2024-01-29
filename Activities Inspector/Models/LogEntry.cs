namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class LogEntry : Entry
    {
        public string Index { get; set; }

        public LogEntry(string index)
        {
            Index = index;
        }
    }
}
