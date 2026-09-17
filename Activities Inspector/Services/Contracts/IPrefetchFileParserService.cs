using Activities_Inspector.Utils;

namespace Activities_Inspector.Services
{
    public interface IPrefetchFileParserService
    {
        IPrefetch Open(string file);
    }
}
