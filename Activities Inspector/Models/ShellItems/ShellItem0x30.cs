using Activities_Inspector.Constants;
using Activities_Inspector.Exceptions;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Activities_Inspector.Models
{
    public class ShellItem0x30 : ShellItemWithExtensions
    {
        public uint FileSize { get; protected set; }
        public ushort FileAttributes { get; protected set; }
        public byte Flags { get; protected set; }
        public ushort ExtensionOffset { get; protected set; }
        public string ShortName { get; protected set; }

        public override DateTime ModifiedDate { get; protected set; }
        public override string TypeName { get => "File Entry"; }

        public override string Name
        {
            get
            {
                // Nome lungo solo da un blocco 0xBEEF0004 valido e non vuoto; altrimenti nome corto 8.3
                var longName = Beef0004?.LongName;
                return !string.IsNullOrEmpty(longName) ? longName : ShortName;
            }

        }

        /// <summary>Blocco 0xBEEF0004 con firma valida, o null se l'item non ne ha (es. Windows XP).</summary>
        protected ExtensionBlockBEEF0004 Beef0004 => ExtensionBlocks.OfType<ExtensionBlockBEEF0004>().FirstOrDefault();

        public override DateTime CreationDate
        {
            get
            {
                var block = Beef0004;
                return block != null ? block.CreationDate : base.CreationDate;
            }
        }
        public override DateTime AccessedDate
        {
            get
            {
                var block = Beef0004;
                return block != null ? block.AccessedDate : base.AccessedDate;
            }
        }

        public ShellItem0x30(byte[] buf)
            : base(buf)
        {

            int off = 0x04;

            Flags = unpack_byte(0x03);

            FileSize = unpack_dword(off);
            off += 4;
            ModifiedDate = unpack_dosdate(off);
            off += 4;
            FileAttributes = unpack_word(off);
            off += 2;
            ExtensionOffset = unpack_word(Size - 2);

            // Gli ultimi 2 byte sono l'offset del primo blocco di estensione solo se cadono dopo il nome e
            // lasciano spazio all'header del blocco; negli item senza estensioni (es. Windows XP) sono byte del nome.
            bool hasExtension = ExtensionOffset > off && ExtensionOffset + 8 <= Size;
            int nameLength = (hasExtension ? ExtensionOffset : Size) - off;

            if (nameLength <= 0)
                ShortName = string.Empty;
            else if ((Type & 0x04) != 0)
                ShortName = unpack_wstring(off, nameLength);
            else
                ShortName = unpack_string(off, nameLength);

            // Il nome termina al primo NUL (dopo possono esserci padding o altri campi)
            ShortName = ShortName.Split('\0')[0];

            if (hasExtension)
            {
                try
                {
                    var block = new ExtensionBlockBEEF0004(buf, ExtensionOffset + offset);
                    if (block.Signature == ExtensionBlockBEEF0004.ExpectedSignature)
                        ExtensionBlocks.Add(block);
                }
                catch (Exception)
                {
                    // Blocco corrotto: resta il nome corto
                }
            }

        }

        public override IDictionary<string, string> GetAllProperties()
        {
            var ret = base.GetAllProperties();
            AddPairIfNotNull(ret, AppConstants.ShellItems.Flags, Flags);
            AddPairIfNotNull(ret, AppConstants.ShellItems.FileSize, FileSize);
            AddPairIfNotNull(ret, AppConstants.ShellItems.FileAttributes, FileAttributes);
            AddPairIfNotNull(ret, AppConstants.ShellItems.ExtensionOffset, ExtensionOffset);
            AddPairIfNotNull(ret, AppConstants.ShellItems.ShortName, ShortName);
            return ret;
        }
    }
}