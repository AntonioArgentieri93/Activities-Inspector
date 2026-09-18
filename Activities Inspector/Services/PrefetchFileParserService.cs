using Activities_Inspector.Constants;
using Activities_Inspector.Utils;
using Activities_Inspector.Versions;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Activities_Inspector.Services
{
    public class PrefetchFileParserService : IPrefetchFileParserService
    {
        public IPrefetch Open(string file)
        {
            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            return Open(fs, file);
        }

        public IPrefetch Open(Stream stream, string file)
        {
            IPrefetch pf = null;

            var rawBytes = new byte[stream.Length];
            stream.Read(rawBytes, 0, (int)rawBytes.Length);

            var tempSig = Encoding.ASCII.GetString(rawBytes, 0, 3);

            if (tempSig.Equals("MAM"))
            {
                var size = BitConverter.ToUInt32(rawBytes, 4);
                var compressedBytes = rawBytes.Skip(8).ToArray();
                var decom = Xpress2.Decompress(compressedBytes, size);
                rawBytes = decom;
            }

            var fileVer = (Utils.Version)BitConverter.ToInt32(rawBytes, AppConstants.Prefetch.VersionOffset);
            var sig = BitConverter.ToInt32(rawBytes, AppConstants.Prefetch.SignatureOffset);

            if (sig == AppConstants.Prefetch.Signature)
            {
                pf = fileVer switch
                {
                    Utils.Version.WinXpOrWin2K3 => new Version17(rawBytes, file),
                    Utils.Version.VistaOrWin7 => new Version23(rawBytes, file),
                    Utils.Version.Win8xOrWin2012x => new Version26(rawBytes, file),
                    Utils.Version.Win10 => new Version30(rawBytes, file),
                    Utils.Version.Win11 => new Version30(rawBytes, file),
                    _ => throw new Exception($"Unknown prefetch version '{fileVer:X}'")
                };
            }

            return pf;
        }
    }
}