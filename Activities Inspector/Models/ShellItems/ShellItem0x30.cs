﻿using ProgettoInformaticaForense_Argentieri.Exceptions;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    /// <summary>
    /// File Entry Shell Item
    /// </summary>
    //Reference for File Entry Shell Types:
    //https://github.com/libyal/libfwsi/blob/master/documentation/Windows%20Shell%20Item%20format.asciidoc#34-file-entry-shell-item
    public class ShellItem0x30 : ShellItemWithExtensions
    {
        public uint FileSize { get; protected set; }
        //https://github.com/libyal/libfwsi/blob/master/documentation/Windows%20Shell%20Item%20format.asciidoc#71-file-attribute-flags
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
                var extensionBlock = ExtensionBlocks.FirstOrDefault();
                if (extensionBlock != null && extensionBlock is ExtensionBlockBEEF0004)
                    return ((ExtensionBlockBEEF0004)extensionBlock).LongName;
                return ShortName;
            }

        }
        public override DateTime CreationDate
        {
            get
            {
                var extensionBlock = ExtensionBlocks.FirstOrDefault();
                if (extensionBlock != null && extensionBlock is ExtensionBlockBEEF0004)
                    return ((ExtensionBlockBEEF0004)extensionBlock).CreationDate;
                return base.CreationDate;
            }
        }
        public override DateTime AccessedDate
        {
            get
            {
                var extensionBlock = ExtensionBlocks.FirstOrDefault();
                if (extensionBlock != null && extensionBlock is ExtensionBlockBEEF0004)
                    return ((ExtensionBlockBEEF0004)extensionBlock).AccessedDate;
                return base.AccessedDate;
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

            if (ExtensionOffset > Size)
                throw new OverrunBufferException(ExtensionOffset, Size);

            if ((Type & 0x04) != 0)
                ShortName = unpack_wstring(off, ExtensionOffset - off);
            else
                ShortName = unpack_string(off, ExtensionOffset - off);

            ExtensionBlockBEEF0004 ExtensionBlock = new ExtensionBlockBEEF0004(buf, ExtensionOffset + offset);
            ExtensionBlocks.Add(ExtensionBlock);

        }

        public override IDictionary<string, string> GetAllProperties()
        {
            var ret = base.GetAllProperties();
            AddPairIfNotNull(ret, Constants.FLAGS, Flags);
            AddPairIfNotNull(ret, Constants.FILE_SIZE, FileSize);
            AddPairIfNotNull(ret, Constants.FILE_ATTRIBUTES, FileAttributes);
            AddPairIfNotNull(ret, Constants.EXTENSION_OFFSET, ExtensionOffset);
            AddPairIfNotNull(ret, Constants.SHORT_NAME, ShortName);
            return ret;
        }
    }
}