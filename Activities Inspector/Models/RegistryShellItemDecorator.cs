using Activities_Inspector.Constants;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public class RegistryShellItemDecorator : IShellItem
    {
        private const string AbsolutePathIdentifier = "AbsolutePath";
        protected IShellItem BaseShellItem { get; }
        protected RegistryKeyWrapper RegKey { get; }

        public RegistryShellItemDecorator(IShellItem shellItem, RegistryKeyWrapper regKey, IShellItem parentShellItem = null)
        {
            BaseShellItem = shellItem ?? throw new ArgumentNullException(nameof(shellItem));
            RegKey = regKey ?? throw new ArgumentNullException(nameof(regKey));
            AbsolutePath = SetAbsolutePath(parentShellItem);
        }

        public ushort Size => BaseShellItem.Size;
        public byte Type => BaseShellItem.Type;
        public string TypeName => BaseShellItem.TypeName ?? string.Empty;
        public string Name => BaseShellItem.Name ?? string.Empty;
        public DateTime ModifiedDate => BaseShellItem.ModifiedDate;
        public DateTime AccessedDate => BaseShellItem.AccessedDate;
        public DateTime CreationDate => BaseShellItem.CreationDate;

        public string AbsolutePath { get; }


        public IDictionary<string, string> GetAllProperties()
        {
            IDictionary<string, string> baseDict = BaseShellItem.GetAllProperties();

            baseDict[AbsolutePathIdentifier] = AbsolutePath;

            if (RegKey.RegistryUser != string.Empty)
                baseDict[AppConstants.ShellItems.RegistryOwner] = RegKey.RegistryUser;
            if (RegKey.RegistryUser != string.Empty)
                baseDict[AppConstants.ShellItems.RegistrySid] = RegKey.RegistrySID;
            if (RegKey.RegistryPath != string.Empty)
                baseDict[AppConstants.ShellItems.RegistryPath] = RegKey.RegistryPath;
            if (RegKey.ShellbagPath != string.Empty)
                baseDict[AppConstants.ShellItems.ShellbagPath] = RegKey.ShellbagPath;
            if (RegKey.LastRegistryWriteDate != DateTime.MinValue)
                baseDict[AppConstants.ShellItems.LastRegWrite] = RegKey.LastRegistryWriteDate.ToString();
            if (RegKey.SlotModifiedDate != DateTime.MinValue)
                baseDict[AppConstants.ShellItems.SlotModifiedDate] = RegKey.SlotModifiedDate.ToString();


            return baseDict;
        }

        protected string SetAbsolutePath(IShellItem parentShellItem)
        {
            if (parentShellItem == null)
                return Name;

            IDictionary<string, string> parentProperties = parentShellItem.GetAllProperties();
            if (parentProperties.TryGetValue(AbsolutePathIdentifier, out string parentPath))
            {
                var candidateAbsolutePath = $@"{parentPath}\{Name}".Replace("\\\\\\", "\\");
                var subStrs = candidateAbsolutePath.Split("\\");

                string absolutePath = string.Empty;

                for (int i = 0; i < subStrs.Length; i++)
                {
                    if (subStrs[i].Contains("?") || subStrs[i].Contains("{")) continue;

                    if (absolutePath == string.Empty)
                    {
                        absolutePath = subStrs[i];
                    }
                    else
                    {
                        absolutePath = absolutePath + "\\" + subStrs[i];
                    }

                }

                return absolutePath.Replace(@"\\", @"\");
            }

            return Name;
        }
    }
}