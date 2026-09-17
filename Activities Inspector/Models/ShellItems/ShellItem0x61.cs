using Activities_Inspector.Constants;
using System;
using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public class ShellItem0x61 : ShellItem
    {
        public string Uri { get; protected set; }
        public string FTPHostname { get; protected set; }
        public string FTPUsername { get; protected set; }
        public string FTPPassword { get; protected set; }

        public uint Flags { get; protected set; }
        public DateTime ConnectionDate { get; protected set; }

        public override string TypeName { get => "URI"; }
        public override string Name
        {
            get
            {
                return FTPHostname ?? Uri;
            }
        }

        public ShellItem0x61(byte[] buf) : base(buf)
        {
            Flags = unpack_dword(0x03);
            int off = 0x04;
            ushort dataSize = unpack_word(off); 
            if (dataSize != 0)
            {
                off += 2; 
                off += 4; 
                off += 4; 
                if (off < Size)
                {
                    ConnectionDate = UnpackFileTime(off); //timestamp in "FILETIME" format (location: 0x0E)
                    off += 8; 
                }
                off += 4;
                off += 12; 
                off += 4;
                if (off < Size) 
                {
                    uint hostnameSize = unpack_dword(off);
                    off += 4; 
                    FTPHostname = unpack_string(off);
                    off += (int)hostnameSize;
                }
                if (off < Size)
                {
                    uint usernameSize = unpack_dword(off);
                    off += 4; 
                    FTPUsername = unpack_string(off);
                    off += (int)usernameSize;

                }
                if (off < Size)
                {
                    uint passwordSize = unpack_dword(off);
                    off += 4; 
                    FTPPassword = unpack_string(off);
                    off += (int)passwordSize; 

                }
                if (off < Size)
                {
                    Uri = unpack_string(off);
                }

            }
        }

        public override IDictionary<string, string> GetAllProperties()
        {
            var ret = base.GetAllProperties();
            AddPairIfNotNull(ret, AppConstants.ShellItems.Uri, Uri);
            AddPairIfNotNull(ret, AppConstants.ShellItems.FtpHostName, FTPHostname);
            AddPairIfNotNull(ret, AppConstants.ShellItems.FtpUserName, FTPUsername);
            AddPairIfNotNull(ret, AppConstants.ShellItems.FtpPassword, FTPPassword);
            AddPairIfNotNull(ret, AppConstants.ShellItems.Flags, Flags);
            AddPairIfNotNull(ret, AppConstants.ShellItems.ConnectionDate, ConnectionDate);
            return ret;
        }
    }
}