using ExtensionBlocks;
using Activities_Inspector.ExtraData;
using Activities_Inspector.ExtraData.ExtraData;
using Activities_Inspector.ShellBags;
using Activities_Inspector.ShellBags.ShellBags;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Activities_Inspector.Utils
{
    public class LnkFile
    {
        [Flags]
        public enum LocationFlag
        {
            [Description("The linked file is on a volume")] VolumeIdAndLocalBasePath = 0x0001,

            [Description("The linked file is on a network share")] CommonNetworkRelativeLinkAndPathSuffix = 0x0002
        }

        public LnkFile(byte[] rawBytes, string sourceFile)
        {
            RawBytes = rawBytes;
            SourceFile = Path.GetFullPath(sourceFile);
            var headerBytes = new byte[76];
            Buffer.BlockCopy(rawBytes, 0, headerBytes, 0, 76);

            Header = new LnkHeader(headerBytes);

            var fi = new FileInfo(sourceFile);
            SourceCreated = new DateTimeOffset(fi.CreationTimeUtc);
            SourceModified = new DateTimeOffset(fi.LastWriteTimeUtc);
            SourceAccessed = new DateTimeOffset(fi.LastAccessTimeUtc);

            if (SourceCreated.Value.Year == 1601)
            {
                SourceCreated = null;
            }

            if (SourceModified.Value.Year == 1601)
            {
                SourceModified = null;
            }

            if (SourceAccessed.Value.Year == 1601)
            {
                SourceAccessed = null;
            }

            var index = 76;

            TargetIDs = new List<ShellBag>();

            if ((Header.DataFlags & LnkHeader.DataFlag.HasTargetIdList) == LnkHeader.DataFlag.HasTargetIdList)
            {
                //process shell items
                var shellItemSize = BitConverter.ToInt16(rawBytes, index);
                index += 2;

                var shellItemBytes = new byte[shellItemSize];
                Buffer.BlockCopy(rawBytes, index, shellItemBytes, 0, shellItemSize);

                var shellItemsRaw = new List<byte[]>();
                var shellItemIndex = 0;

                while (shellItemIndex < shellItemBytes.Length)
                {
                    var shellSize = BitConverter.ToUInt16(shellItemBytes, shellItemIndex);

                    if (shellSize == 0)
                    {
                        break;
                    }
                    var itemBytes = new byte[shellSize];
                    Buffer.BlockCopy(shellItemBytes, shellItemIndex, itemBytes, 0, shellSize);

                    shellItemsRaw.Add(itemBytes);
                    shellItemIndex += shellSize;
                }

                foreach (var shellItem in shellItemsRaw)
                {
                    try
                    {
                        TargetIDs.Add(ShellItemFactory.Create(shellItem));
                    }
                    catch
                    {
                        // Un singolo item malformato o di tipo sconosciuto non
                        // deve invalidare l'intero file: lo si salta e si conta.
                        SkippedShellItems++;
                    }
                }

                index += shellItemSize;
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasLinkInfo) == LnkHeader.DataFlag.HasLinkInfo)
            {
                if (!TryParseLinkInfo(rawBytes, ref index))
                    index = rawBytes.Length;
            }

            var isUnicode = (Header.DataFlags & LnkHeader.DataFlag.IsUnicode) == LnkHeader.DataFlag.IsUnicode;

            if ((Header.DataFlags & LnkHeader.DataFlag.HasName) == LnkHeader.DataFlag.HasName)
            {
                Name = ReadLengthPrefixedString(rawBytes, ref index, isUnicode);
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasRelativePath) == LnkHeader.DataFlag.HasRelativePath)
            {
                RelativePath = ReadLengthPrefixedString(rawBytes, ref index, isUnicode);
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasWorkingDir) == LnkHeader.DataFlag.HasWorkingDir)
            {
                WorkingDirectory = ReadLengthPrefixedString(rawBytes, ref index, isUnicode);
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasArguments) == LnkHeader.DataFlag.HasArguments)
            {
                Arguments = ReadLengthPrefixedString(rawBytes, ref index, isUnicode);
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasIconLocation) == LnkHeader.DataFlag.HasIconLocation)
            {
                IconLocation = ReadLengthPrefixedString(rawBytes, ref index, isUnicode);
            }


            var extraByteBlocks = new List<byte[]>();

            while (index < rawBytes.Length)
            {
                var extraSize = BitConverter.ToInt32(rawBytes, index);
                if (extraSize == 0)
                {
                    break;
                }

                if (extraSize > rawBytes.Length - index)
                {
                    extraSize = rawBytes.Length - index;
                }

                var extraBytes = new byte[extraSize];
                Buffer.BlockCopy(rawBytes, index, extraBytes, 0, extraSize);

                extraByteBlocks.Add(extraBytes);

                index += extraSize;
            }

            ExtraBlocks = new List<ExtraDataBase>();

            foreach (var extraBlock in extraByteBlocks)
            {
                ExtraBlocks.Add(ExtraDataBlockFactory.Create(extraBlock));
            }
        }

        private bool TryParseLinkInfo(byte[] rawBytes, ref int index)
        {
            try
            {
                if (index + 4 > rawBytes.Length) return false;

                var locationItemSize = BitConverter.ToInt32(rawBytes, index);
                if (locationItemSize <= 0 || index + locationItemSize > rawBytes.Length) return false;

                var locationBytes = new byte[locationItemSize];
                Buffer.BlockCopy(rawBytes, index, locationBytes, 0, locationItemSize);

                if (locationBytes.Length > 20)
                {
                    var locationInfoHeaderSize = BitConverter.ToInt32(locationBytes, 4);

                    LocationFlags = (LocationFlag)BitConverter.ToInt32(locationBytes, 8);

                    var volOffset = BitConverter.ToInt32(locationBytes, 12);
                    var vbyteSize = BitConverter.ToInt32(locationBytes, volOffset);
                    var volBytes = new byte[vbyteSize];
                    Buffer.BlockCopy(locationBytes, volOffset, volBytes, 0, vbyteSize);

                    if (volOffset > 0)
                    {
                        VolumeInfo = new LnkVolumeInfo(volBytes);
                    }

                    var localPathOffset = BitConverter.ToInt32(locationBytes, 16);
                    var networkShareOffset = BitConverter.ToInt32(locationBytes, 20);

                    if ((LocationFlags & LocationFlag.VolumeIdAndLocalBasePath) ==
                        LocationFlag.VolumeIdAndLocalBasePath)
                    {
                        LocalPath = CodePagesEncodingProvider.Instance.GetEncoding(1252)
                            .GetString(locationBytes, localPathOffset, locationBytes.Length - localPathOffset)
                            .Split('\0')
                            .First();
                    }
                    if ((LocationFlags & LocationFlag.CommonNetworkRelativeLinkAndPathSuffix) ==
                        LocationFlag.CommonNetworkRelativeLinkAndPathSuffix)
                    {
                        var networkShareSize = BitConverter.ToInt32(locationBytes, networkShareOffset);
                        var networkBytes = new byte[networkShareSize];
                        Buffer.BlockCopy(locationBytes, networkShareOffset, networkBytes, 0, networkShareSize);

                        NetworkShareInfo = new NetworkShareInfo(networkBytes);
                    }

                    var commonPathOffset = BitConverter.ToInt32(locationBytes, 24);

                    CommonPath = CodePagesEncodingProvider.Instance.GetEncoding(1252)
                        .GetString(locationBytes, commonPathOffset, locationBytes.Length - commonPathOffset)
                        .Split('\0')
                        .First();

                    if (locationInfoHeaderSize > 28)
                    {
                        var uniLocalOffset = BitConverter.ToInt32(locationBytes, 28);

                        var unicodeLocalPath = Encoding.Unicode
                            .GetString(locationBytes, uniLocalOffset, locationBytes.Length - uniLocalOffset)
                            .Split('\0')
                            .First();
                        LocalPath = unicodeLocalPath;
                    }

                    if (locationInfoHeaderSize > 32)
                    {
                        var uniCommonOffset = BitConverter.ToInt32(locationBytes, 32);

                        var unicodeCommonPath = Encoding.Unicode
                            .GetString(locationBytes, uniCommonOffset, locationBytes.Length - uniCommonOffset)
                            .Split('\0')
                            .First();
                        CommonPath = unicodeCommonPath;
                    }
                }

                index += locationItemSize;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ReadLengthPrefixedString(byte[] rawBytes, ref int index, bool isUnicode)
        {
            try
            {
                if (index + 2 > rawBytes.Length) return string.Empty;

                var length = BitConverter.ToInt16(rawBytes, index);
                index += 2;

                string value;
                if (isUnicode)
                {
                    value = Encoding.Unicode.GetString(rawBytes, index, length * 2);
                    index += length;
                }
                else
                {
                    value = Utils.EncodingProvider.Ansi.GetString(rawBytes, index, length);
                }
                index += length;

                return value;
            }
            catch
            {
                return string.Empty;
            }
        }

        public List<ShellBag> TargetIDs { get; }

        public int SkippedShellItems { get; private set; }

        public List<ExtraDataBase> ExtraBlocks { get; }


        public DateTimeOffset? SourceCreated { get; }
        public DateTimeOffset? SourceModified { get; }
        public DateTimeOffset? SourceAccessed { get; }

        public string CommonPath { get; private set; }
        public string LocalPath { get; private set; }
        public LnkVolumeInfo VolumeInfo { get; private set; }
        public NetworkShareInfo NetworkShareInfo { get; private set; }
        public string SourceFile { get; }

        public byte[] RawBytes { get; }
        public LnkHeader Header { get; }

        public string Name { get; }
        public string RelativePath { get; }
        public string WorkingDirectory { get; }
        public string Arguments { get; }
        public string IconLocation { get; }

        public LocationFlag LocationFlags { get; private set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Source file: {SourceFile}");
            sb.AppendLine($"Source created: {SourceCreated}");
            sb.AppendLine($"Source modified: {SourceModified}");
            sb.AppendLine($"Source accessed: {SourceAccessed}");
            sb.AppendLine();
            sb.AppendLine("--- Header ---");
            sb.AppendLine($"  File size: {Header.FileSize:N0}");
            sb.AppendLine($"  Flags: {Header.DataFlags}");
            sb.AppendLine($"  File attributes: {Header.FileAttributes}");

            if (Header.HotKey.Length > 0)
            {
                sb.AppendLine($"  Hot key: {Header.HotKey}");
            }

            sb.AppendLine($"  Icon index: {Header.IconIndex}");
            sb.AppendLine(
                $"  Show window: {Header.ShowWindow} ({Helpers.GetDescriptionFromEnumValue(Header.ShowWindow)})");
            sb.AppendLine($"  Target created: {Header.TargetCreationDate}");
            sb.AppendLine($"  Target modified: {Header.TargetLastAccessedDate}");
            sb.AppendLine($"  Target accessed: {Header.TargetModificationDate}");


            if (TargetIDs.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- Target ID information ---");
                foreach (var shellBag in TargetIDs)
                {
                    sb.Append($">>{shellBag}");
                }
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasLinkInfo) == LnkHeader.DataFlag.HasLinkInfo)
            {
                sb.AppendLine();
                sb.AppendLine("--- Link information ---");
                sb.AppendLine($"Location flags: {LocationFlags}");

                if (VolumeInfo != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("Volume information");
                    sb.AppendLine($"Drive type: {VolumeInfo.DriveType}");
                    sb.AppendLine($"Serial number: {VolumeInfo.VolumeSerialNumber}");

                    var label = VolumeInfo.VolumeLabel.Length > 0 ? VolumeInfo.VolumeLabel : "(No label)";

                    sb.AppendLine($"Label: {label}");
                }

                if (LocalPath?.Length > 0)
                {
                    sb.AppendLine($"Local path: {LocalPath}");
                }

                if (NetworkShareInfo != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("Network share information");

                    if (NetworkShareInfo.DeviceName.Length > 0)
                    {
                        sb.AppendLine($"Device name: {NetworkShareInfo.DeviceName}");
                    }

                    sb.AppendLine($"Share name: {NetworkShareInfo.NetworkShareName}");

                    sb.AppendLine($"Provider type: {NetworkShareInfo.NetworkProviderType}");
                    sb.AppendLine($"Share flags: {NetworkShareInfo.ShareFlags}");
                }

                if (CommonPath.Length > 0)
                {
                    sb.AppendLine($"Common path: {CommonPath}");
                }
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasName) == LnkHeader.DataFlag.HasName)
            {
                sb.AppendLine($"Name: {Name}");
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasRelativePath) == LnkHeader.DataFlag.HasRelativePath)
            {
                sb.AppendLine($"Relative Path: {RelativePath}");
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasWorkingDir) == LnkHeader.DataFlag.HasWorkingDir)
            {
                sb.AppendLine($"Working Directory: {WorkingDirectory}");
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasArguments) == LnkHeader.DataFlag.HasArguments)
            {
                sb.AppendLine($"Arguments: {Arguments}");
            }

            if ((Header.DataFlags & LnkHeader.DataFlag.HasIconLocation) == LnkHeader.DataFlag.HasIconLocation)
            {
                sb.AppendLine($"Icon Location: {IconLocation}");
            }

            if (ExtraBlocks.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- Extra blocks information ---");
                foreach (var extraDataBase in ExtraBlocks)
                {
                    sb.AppendLine($">>{extraDataBase}");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
