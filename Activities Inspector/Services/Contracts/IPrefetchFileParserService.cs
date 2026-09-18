using Activities_Inspector.Utils;
using System.IO;

namespace Activities_Inspector.Services
{
    public interface IPrefetchFileParserService
    {
        IPrefetch Open(string file);

        IPrefetch Open(Stream stream, string file);
    }
}
