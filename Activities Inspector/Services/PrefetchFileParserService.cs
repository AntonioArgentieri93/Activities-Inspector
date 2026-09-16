using ProgettoInformaticaForense_Argentieri.Utils;
using ProgettoInformaticaForense_Argentieri.Versions;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    /// <summary>
    /// https://binaryforay.blogspot.com/2016/01/windows-prefetch-parser-in-c.html
    /// </summary>
    public class PrefetchFileParserService : IPrefetchFileParserService
    {
        private const int SIGNATURE = 0x41434353;

        public IPrefetch Open(string file)
        {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                return Open(fs, file);
            }
        }

        private IPrefetch Open(Stream stream, string file)
        {
            IPrefetch pf = null;

            var rawBytes = new byte[stream.Length];
            stream.Read(rawBytes, (int)0, (int)(rawBytes.Length));

            var tempSig = Encoding.ASCII.GetString(rawBytes, 0, 3);

            if (tempSig.Equals("MAM"))
            {
                //windows 10, so we need to decompress

                var size = BitConverter.ToUInt32(rawBytes, 4);

                //get our compressed bytes (skipping signature and uncompressed size)
                var compressedBytes = rawBytes.Skip(8).ToArray();
                var decom = Xpress2.Decompress(compressedBytes, size);

                rawBytes = decom;
            }

            var fileVer = (Utils.Version)BitConverter.ToInt32(rawBytes, 0);

            var sig = BitConverter.ToInt32(rawBytes, 4);

            if(sig == SIGNATURE)
            {
                switch (fileVer)
                {
                    case Utils.Version.WinXpOrWin2K3:
                        pf = new Version17(rawBytes, file);
                        break;
                    case Utils.Version.VistaOrWin7:
                        pf = new Version23(rawBytes, file);
                        break;
                    case Utils.Version.Win8xOrWin2012x:
                        pf = new Version26(rawBytes, file);
                        break;
                    case Utils.Version.Win10:
                    case Utils.Version.Win11:
                        pf = new Version30(rawBytes, file);
                        break;
                    default:
                        throw new Exception($"Unknown version '{fileVer:X}'");
                }
            }

            return pf;
        }
    }
}
