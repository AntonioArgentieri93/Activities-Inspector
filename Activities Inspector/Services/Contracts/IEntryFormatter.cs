using Activities_Inspector.Models;

namespace Activities_Inspector.Services
{
    public interface IEntryFormatter
    {
        string AsCsv(Entry entry);
    }
}
