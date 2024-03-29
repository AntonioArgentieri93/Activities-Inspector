﻿using NLua;
using ProgettoInformaticaForense_Argentieri.Exceptions;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class LuaShellItem : ShellItem
    {
        private readonly int identifier;
        private readonly Lua state;
        private readonly string luascript;
        Dictionary<string, string> properties = new Dictionary<string, string>();
        private bool skipParsing = false;

        public LuaShellItem(byte[] buf, int identifier, string luascript) : base(buf)
        {
            try
            {
                this.identifier = identifier;
                state = new Lua();
                state.LoadCLRPackage();
                state.DoString(@" import ('System', 'SeeShells', 'SeeShells.ShellParser.ShellItems', 'SeeShells.ShellParser') ");
                state[Constants.PROPERTIES] = properties;
                state[Constants.SHELL_ITEM] = this;
                state[Constants.KNOWN_GUIDS] = KnownGuids.dict;
                this.luascript = luascript;
            }
            catch (BadImageFormatException)
            {
                skipParsing = true;
            }

        }

        public override string TypeName
        {
            get
            {
                return GetString(Constants.TYPENAME);
            }
        }


        public override string Name
        {
            get
            {
                return GetString(Constants.NAME);
            }
        }

        public override DateTime ModifiedDate
        {
            get
            {
                return GetDate(Constants.MODIFIED_DATE);
            }
        }

        public override DateTime AccessedDate
        {
            get
            {
                return GetDate(Constants.ACCESSED_DATE);
            }
        }

        public override DateTime CreationDate
        {
            get
            {
                return GetDate(Constants.CREATION_DATE);
            }
        }

        private string GetString(string propertyName)
        {
            string result;

            if (!PropertiesAreParsedAlready())
                return null;

            try
            {
                GetAllProperties().TryGetValue(propertyName, out string typename);
                result = typename;
            }
            catch (ArgumentNullException)
            {
                result = "";
            }

            return result;
        }

        private DateTime GetDate(string propertyName)
        {
            DateTime date;

            if (!PropertiesAreParsedAlready())
                return DateTime.MinValue;

            try
            {
                GetAllProperties().TryGetValue(propertyName, out string dateString);
                date = DateTime.Parse(dateString);
            }
            catch (ArgumentNullException)
            {
                date = DateTime.MinValue;
            }
            catch (FormatException)
            {
                date = DateTime.MinValue;
            }

            return date;
        }

        public override IDictionary<string, string> GetAllProperties()
        {
            if (!PropertiesAreParsedAlready())
            {
                var prop = base.GetAllProperties();

                try
                {
                    ParseShellItem();
                }
                catch (Exception)
                {
                    skipParsing = true;
                }
            }

            return properties;
        }

        private bool PropertiesAreParsedAlready()
        {
            if (properties.Count > 0)
                return true;

            return false;
        }

        private void ParseShellItem()
        {
            if (skipParsing)
                return;

            if (luascript is null || luascript == "")
                throw new Exception("No Lua script provided");

            if (!luascript.Contains("properties:Add"))
                throw new Exception("There is no properties table in this Lua script to get information from");

            try
            {
                state.DoString(@luascript);
            }
            catch (Exception)
            { }


            state.Dispose();
        }

        public new byte unpack_byte(int off)
        {
            try
            {
                return buf[offset + off];
            }
            catch (IndexOutOfRangeException)
            {
                throw new OverrunBufferException(offset + off, buf.Length);
            }
        }
        public new ushort unpack_word(int off)
        {
            try
            {
                return BitConverter.ToUInt16(buf, offset + off);
            }
            catch (IndexOutOfRangeException)
            {
                throw new OverrunBufferException(offset + off, buf.Length);
            }
        }
        public new string unpack_guid(int off)
        {
            try
            {
                return string.Format("{0:x2}{1:x2}{2:x2}{3:x2}-{4:x2}{5:x2}-{6:x2}{7:x2}-{8:x2}{9:x2}-{10:x2}{11:x2}{12:x2}{13:x2}{14:x2}{15:x2}",
                    buf[offset + off + 3], buf[offset + off + 2], buf[offset + off + 1], buf[offset + off],
                    buf[offset + off + 5], buf[offset + off + 4],
                    buf[offset + off + 7], buf[offset + off + 6],
                    buf[offset + off + 8], buf[offset + off + 9],
                    buf[offset + off + 10], buf[offset + off + 11], buf[offset + off + 12], buf[offset + off + 13], buf[offset + off + 14], buf[offset + off + 15]);
            }
            catch (IndexOutOfRangeException)
            {
                throw new OverrunBufferException(offset + off, buf.Length);
            }
        }
        public new string unpack_wstring(int off, int length = 0)
        {
            try
            {
                if (length == 0)
                {
                    int end = offset + off;
                    for (int ind = offset + off; ind + 1 < buf.Length; ind += 2)
                    {
                        if (buf[ind] == 0 && buf[ind + 1] == 0)
                        {
                            end = ind;
                            break;
                        }
                    }
                    length = end - offset - off;
                }
                while (buf[offset + off + length - 2] == 0 && buf[offset + off + length - 1] == 0) length -= 2;
                return Encoding.Unicode.GetString(buf, offset + off, length);
            }
            catch (IndexOutOfRangeException)
            {
                throw new OverrunBufferException(offset + off, buf.Length);
            }
        }
        public new string unpack_string(int off, int length = 0)
        {
            try
            {
                if (length == 0)
                {
                    int end = Array.IndexOf(buf, (byte)0, offset + off);
                    length = end - offset - off;
                    if (length == 0) return string.Empty;
                }
                while (buf[offset + off + length - 1] == 0) --length;
                return Encoding.ASCII.GetString(buf, offset + off, length);
            }
            catch (IndexOutOfRangeException)
            {
                throw new OverrunBufferException(offset + off, buf.Length);
            }
        }
        public new uint unpack_dword(int off)
        {
            try
            {
                return BitConverter.ToUInt32(buf, offset + off);
            }
            catch (IndexOutOfRangeException)
            {
                throw new OverrunBufferException(offset + off, buf.Length);
            }
        }
        public new ulong UnpackQword(int off)
        {
            try
            {
                return BitConverter.ToUInt64(buf, offset + off);
            }
            catch (IndexOutOfRangeException)
            {
                throw new OverrunBufferException(offset + off, buf.Length);
            }
        }
        public new DateTime unpack_dosdate(int off)
        {
            try
            {
                ushort dosdate = (ushort)(buf[offset + off + 1] << 8 | buf[offset + off]);
                ushort dostime = (ushort)(buf[offset + off + 3] << 8 | buf[offset + off + 2]);

                if ((dosdate == 0 || dosdate == 1) && dostime == 0)
                {
                    return DateTime.MinValue; 
                }

                int day = dosdate & 0x1F;
                int month = (dosdate & 0x1E0) >> 5;
                int year = (dosdate & 0xFE00) >> 9;
                year += 1980;

                int sec = (dostime & 0x1F) * 2;
                int minute = (dostime & 0x7E0) >> 5;
                int hour = (dostime & 0xF800) >> 11;

                return new DateTime(year, month, day, hour, minute, sec);
            }
            catch (IndexOutOfRangeException)
            {
                throw new OverrunBufferException(offset + off, buf.Length);
            }
        }
        public new DateTime UnpackFileTime(int off)
        {
            return DateTime.FromFileTimeUtc(BitConverter.ToInt64(buf, offset + off));
        }
        public new int align(int off, int alignment)
        {
            if (off % alignment == 0)
                return off;
            return off + (alignment - off % alignment);
        }
    }
}
