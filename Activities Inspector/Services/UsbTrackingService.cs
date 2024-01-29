using Activities_Inspector.Models;
using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utils;
using RawCopy;
using Registry;
using Registry.Abstractions;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class UsbTrackingService : IUsbTrackingService
    {
        private string _hivePath = @"C:\Windows\System32\config\SYSTEM";
        private string _regPath = @"SYSTEM";

        public async Task<Result<List<UsbEntry>>> BuildUsbEntries(bool isAdministrator)
        {
            var taskCompletionSource = new TaskCompletionSource<Result<List<UsbEntry>>> (
                TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                var tmp = new List<UsbEntry>();

                await Task.Run(() =>
                {
                    if (isAdministrator)
                    {
                        var files = new List<string>()
                        {
                            _hivePath
                        };

                        var rawFiles = Helper.GetRawFiles(files);

                        var bb = rawFiles.First().FileStream.ReadFully();

                        var reg = new RegistryHive(bb, rawFiles.First().InputFilename);

                        reg.ParseHive();

                        var subKeys = reg.Root.SubKeys;
                        var controlSets = subKeys.Where(sk => sk.KeyName.StartsWith("ControlSet")).ToList();

                        foreach (var controlSet in controlSets)
                        {
                            var keyPath = controlSet.KeyPath + @"\Enum\USB";
                            var key = reg.GetKey(keyPath);

                            if (key != null)
                            {
                                foreach (var registryKey in key.SubKeys)
                                {
                                    string keyName = registryKey.KeyName;

                                    if (keyName.StartsWith("ROOT")) continue;

                                    var vendorId = BuildVendorId(keyName);
                                    var productId = BuildProductId(keyName);

                                    var plugged = IsPlugged(vendorId, productId);

                                    foreach (var subKey in registryKey.SubKeys)
                                    {
                                        var serialNumber = subKey.KeyName;

                                        var registryValue = subKey.GetValue("DeviceDesc").ToString();
                                        var deviceName = BuildDeviceName(registryValue);

                                        var usbClassRegistryValue = (string)subKey.GetValue("CompatibleIDs");
                                        var usbClass = BuildUsbClass(usbClassRegistryValue);

                                        var lastConnected = GetLocalDateTime(
                                             GetData(subKey, "{83da6326-97a6-4088-9453-a1923f573b29}", "0066")
                                         );

                                        var lastRemoved = GetLocalDateTime(
                                            GetData(subKey, "{83da6326-97a6-4088-9453-a1923f573b29}", "0067")
                                        );

                                        var candidateUsbEntry = new UsbEntry(plugged, deviceName, serialNumber, vendorId, productId,
                                            usbClass, lastConnected, lastRemoved);

                                        var isDuplicate = tmp.Any(ue => ue.Equals(candidateUsbEntry));

                                        if (isDuplicate == false)
                                        {
                                            tmp.Add(candidateUsbEntry);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        using(var baseKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(_regPath))
                        {
                            var subKeyNames = baseKey.GetSubKeyNames();
                            var controlSets = subKeyNames.Where(sk => sk.StartsWith("ControlSet")).ToList();

                            foreach (var controlSet in controlSets)
                            {
                                var keyPath = controlSet + @"\Enum\USB";
                                using(var key = baseKey.OpenSubKey(keyPath))
                                {
                                    if(key != null)
                                    {
                                        foreach (var registryKey in key.GetSubKeyNames())
                                        {
                                            if (registryKey.StartsWith("ROOT")) continue;

                                            var vendorId = BuildVendorId(registryKey);
                                            var productId = BuildProductId(registryKey);

                                            var plugged = IsPlugged(vendorId, productId);

                                            using (var skey = key.OpenSubKey(registryKey))
                                            {
                                                if(skey != null)
                                                {
                                                    foreach(var sskeyName in skey.GetSubKeyNames())
                                                    {
                                                        var serialNumber = sskeyName;

                                                        using (var ssKey = skey.OpenSubKey(sskeyName))
                                                        {
                                                            if(ssKey != null)
                                                            {
                                                                var registryValue = ssKey.GetValue("DeviceDesc").ToString();
                                                                var deviceName = BuildDeviceName(registryValue);

                                                                var usbClassRegistryValue = (string[])ssKey.GetValue("CompatibleIDs");
                                                                var joinStr = string.Join(" ", usbClassRegistryValue);
                                                                var usbClass = BuildUsbClass(joinStr);

                                                                var candidateUsbEntry = new UsbEntry(plugged, deviceName, serialNumber,
                                                                    vendorId, productId, usbClass);
                                                                var isDuplicate = tmp.Any(ue => ue.Equals(candidateUsbEntry));

                                                                if (isDuplicate == false)
                                                                {
                                                                    tmp.Add(candidateUsbEntry);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    taskCompletionSource.SetResult(Result.Success(tmp));
                });
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetResult(Result.Failure<List<UsbEntry>>(ex.Message));
            }

            return taskCompletionSource.Task.Result;
        }

        private string BuildVendorId(string registryKey)
        {
            if (string.IsNullOrEmpty(registryKey)) return string.Empty;

            var strings = registryKey.Split("&");
            return strings[0].Remove(0, 4);
        }

        private string BuildProductId(string registryKey)
        {
            if (string.IsNullOrEmpty(registryKey)) return string.Empty;

            var strings = registryKey.Split("&");
            return strings[1].Remove(0, 4);
        }

        private string BuildDeviceName(string registryValue)
        {
            if (string.IsNullOrEmpty(registryValue)) return string.Empty;

            if (registryValue.Contains(";"))
            {
                var result = registryValue.Split(";");
                var finalResult = result[result.Length - 1];

                return finalResult;
            }
            else
            {
                return registryValue;
            }
        }

        private string BuildUsbClass(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var devFilter = "DevClass_";
            var classFilter = "Class_";

            var replacedValue = value.Replace("USB\\", string.Empty);

            if (replacedValue.Contains(devFilter))
            {
                var subValues = replacedValue.Split("&");
                var usbClassString = subValues.First(sv => sv.Contains(devFilter)).Replace(devFilter, string.Empty).ToLower();
                var usbClass = MapUsbClass(usbClassString);

                return usbClass;
            }
            else if (replacedValue.Contains(classFilter))
            {
                var subValues = replacedValue.Split("&");
                var usbClassString = subValues.First(sv => sv.Contains(classFilter)).Replace(classFilter, string.Empty).ToLower();
                var usbClass = MapUsbClass(usbClassString);

                return usbClass;
            }

            return "Unknown USB Device";
        }

        private static string MapUsbClass(string baseClass)
        {
            if (string.IsNullOrEmpty(baseClass)) return string.Empty;

            if (UsbClass.UsbClasses.ContainsKey(baseClass) == false) return string.Empty;

            return UsbClass.UsbClasses[baseClass];
        }

        private byte[] GetData(RegistryKey serialSubKey, string guidValue, string numValue)
        {
            var properties = serialSubKey.SubKeys.SingleOrDefault(t => t.KeyName == "Properties");
            if (properties == null)
                return null;

            var GUID = properties.SubKeys.SingleOrDefault(t => t.KeyName == guidValue);
            if (GUID == null)
                return null;

            var subKey = GUID.SubKeys.SingleOrDefault(t => t.KeyName == numValue);
            if (subKey == null)
                return null;

            return subKey.Values.SingleOrDefault(t => t.ValueName == "(default)")?.ValueDataRaw;
        }

        private DateTimeOffset? GetLocalDateTime(byte[] data)
        {
            if (data == null || data.Length != 8)
                return null;

            return DateTimeOffset.FromFileTime(BitConverter.ToInt64(data, 0)).ToLocalTime();
        }

        private bool IsPlugged(string vid, string pid)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))
                {
                    using (var collection = searcher.Get())
                    {
                        foreach (var device in collection)
                        {
                            var usbDevice = Convert.ToString(device);

                            Console.WriteLine(device);

                            if (usbDevice.Contains(vid) && usbDevice.Contains(pid))
                                return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception) 
            {
                return false;
            }  
        }
    }
}
