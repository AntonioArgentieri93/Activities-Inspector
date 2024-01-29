using ExtensionBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgettoInformaticaForense_Argentieri.Utility
{
    public class ShellBagZipContents : ShellBag
    {
        public ShellBagZipContents(byte[] rawBytes)
        {
            ExtensionBlocks = new List<IExtensionBlock>();

            FriendlyName = "Zip file contents";

            var rawdate = rawBytes.Skip(0x24).Take(40).ToArray();

            var rawdatestring = Encoding.Unicode.GetString(rawdate).Split('\0').First();
            LastAccessTimeString = rawdatestring;

            DateTimeOffset lastaccess;

            if (DateTimeOffset.TryParse(rawdatestring, out lastaccess))
            {
                LastAccessTime = lastaccess;
            }
            else
            {
                rawdate = rawBytes.Skip(0x18).Take(40).ToArray();
                rawdatestring = Encoding.Unicode.GetString(rawdate).Split('\0').First();

                LastAccessTimeString = rawdatestring;

                if (DateTimeOffset.TryParse(rawdatestring, out lastaccess))
                {
                    LastAccessTime = lastaccess;
                }
            }

            var index = 84;

            if (rawBytes[0x14] == 0x10) //xp hackz
            {
                index = 60;
            }

            try
            {
                var nameSize1 = BitConverter.ToUInt16(rawBytes, index);
                index += 4;

                var nameSize2 = BitConverter.ToUInt16(rawBytes, index);
                index += 4;

                Value = "!!!Unable to determine value!!!";

                if (nameSize1 > 0)
                {
                    var folderName1 = Encoding.Unicode.GetString(rawBytes, index, nameSize1 * 2);
                    index += nameSize1 * 2;

                    Value = folderName1;

                    index += 2; // skip end of unicode string
                }

                if (nameSize2 > 0)
                {
                    var folderName2 = Encoding.Unicode.GetString(rawBytes, index, nameSize2 * 2);
                    index += nameSize2 * 2;

                    index += 2; // skip end of unicode string
                }
            }
            catch (Exception)
            {
                index = 60;
                var nameSize1 = BitConverter.ToUInt16(rawBytes, index);
                index += 4;

                var nameSize2 = BitConverter.ToUInt16(rawBytes, index);
                index += 4;

                if (nameSize1 > 0)
                {
                    var folderName1 = Encoding.Unicode.GetString(rawBytes, index, nameSize1 * 2);
                    index += nameSize1 * 2;

                    Value = folderName1;

                    index += 2; // skip end of unicode string
                }

                if (nameSize2 > 0)
                {
                    var folderName2 = Encoding.Unicode.GetString(rawBytes, index, nameSize2 * 2);
                    index += nameSize2 * 2;

                    index += 2; // skip end of unicode string
                }
            }
        }

        /// <summary>
        ///     Last access time of BagPath
        /// </summary>
        public DateTimeOffset? LastAccessTime { get; }

        public string LastAccessTimeString { get; }

        public override string ToString()
        {
            var sb = new StringBuilder();


            if (LastAccessTimeString.Equals("N/A") == false)
            {
                sb.AppendLine($"Last access internal value: {LastAccessTimeString}");
                sb.AppendLine();
            }

            sb.AppendLine(base.ToString());

            return sb.ToString();
        }
    }
}
