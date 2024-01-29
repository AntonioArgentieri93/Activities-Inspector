using System;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Utils
{
    public class VolumeInfo
    {
        public VolumeInfo(int offset, DateTimeOffset createdOn, string serialNum, string deviceName)
        {
            DeviceOffset = offset;
            CreationTime = createdOn;
            SerialNumber = serialNum;
            DeviceName = deviceName;

            FileReferences = new List<MFTInformation>();
            DirectoryNames = new List<string>();
        }

        public int DeviceOffset { get; }
        public DateTimeOffset CreationTime { get; }
        public string SerialNumber { get; }
        public string DeviceName { get; }

        public List<MFTInformation> FileReferences { get; }

        public List<string> DirectoryNames { get; }
    }
}
