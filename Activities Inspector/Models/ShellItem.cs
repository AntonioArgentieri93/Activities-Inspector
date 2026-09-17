using Activities_Inspector.Constants;
using System;
using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public class ShellItem : Block, IShellItem
    {
        public virtual ushort Size { get; protected set; }
        public virtual byte Type { get; protected set; }

        public virtual string TypeName
        {
            get
            {
                return "Unknown";
            }
            protected set
            {}
        }

        public virtual string Name
        {
            get
            {
                return "??";
            }
            protected set
            {}
        }
        public virtual DateTime ModifiedDate
        {
            get
            {
                return DateTime.MinValue;
            }
            protected set
            {}
        }
        public virtual DateTime AccessedDate
        {
            get
            {
                return DateTime.MinValue;
            }
            protected set
            {}
        }
        public virtual DateTime CreationDate
        {
            get
            {
                return DateTime.MinValue;
            }
            protected set
            {}
        }
        public ShellItem(byte[] buf, int offset)
            : base(buf, offset)
        {
            Type = unpack_byte(0x02);
            Size = unpack_word(0x00);
        }

        public ShellItem(byte[] buf) : base(buf, 0)
        {
            Type = unpack_byte(0x02);
            Size = unpack_word(0x00);
        }

        public virtual IDictionary<string, string> GetAllProperties()
        {
            SortedDictionary<string, string> properties = new SortedDictionary<string, string>();
            AddPairIfNotNull(properties, AppConstants.ShellItems.Size, Size.ToString("X2"));
            AddPairIfNotNull(properties, AppConstants.ShellItems.Type, Type.ToString("X2"));
            AddPairIfNotNull(properties, AppConstants.ShellItems.TypeName, TypeName);
            AddPairIfNotNull(properties, AppConstants.ShellItems.Name, Name);
            AddPairIfNotNull(properties, AppConstants.ShellItems.ModifiedDate, ModifiedDate);
            AddPairIfNotNull(properties, AppConstants.ShellItems.AccessedDate, AccessedDate);
            AddPairIfNotNull(properties, AppConstants.ShellItems.CreationDate, CreationDate);
            return properties;
        }

    }
}