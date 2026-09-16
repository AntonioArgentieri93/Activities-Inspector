using Activities_Inspector.Models;
using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Constants;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utils;
using RawCopy;
using Registry;
using Registry.Abstractions;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class UsbTrackingService : IUsbTrackingService
    {
        public async Task<Result<List<UsbEntry>>> BuildUsbEntriesAsync(bool isAdministrator, CancellationToken cancellationToken = default)
        {
            try
            {
                var entries = new List<UsbEntry>();

                if (isAdministrator)
                {
                    entries = await BuildUsbEntriesFromHiveAsync(cancellationToken);
                }
                else
                {
                    entries = await BuildUsbEntriesFromRegistryAsync(cancellationToken);
                }

                return Result.Success(entries);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result.Failure<List<UsbEntry>>(ex.ToString());
            }
        }

        private async Task<List<UsbEntry>> BuildUsbEntriesFromHiveAsync(CancellationToken cancellationToken)
        {
            var entries = new List<UsbEntry>();

            var files = new List<string> { AppConstants.Paths.SystemHivePath };
            var rawFiles = Helper.GetRawFiles(files);
            var rawFile = rawFiles.First();

            var byteArray = await rawFile.FileStream.ReadFullyAsync();
            var reg = new RegistryHive(byteArray, rawFile.InputFilename);
            _ = reg.ParseHive();

            var subKeys = reg.Root.SubKeys;
            var controlSets = subKeys.Where(sk => sk.KeyName.StartsWith(AppConstants.Registry.ControlSetPrefix)).ToList();

            foreach (var controlSet in controlSets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyPath = controlSet.KeyPath + @"\" + AppConstants.Registry.UsbEnumPath;
                var key = reg.GetKey(keyPath);

                if (key != null)
                {
                    ProcessUsbKeys(key.SubKeys, entries, cancellationToken);
                }
            }

            return entries;
        }

        private async Task<List<UsbEntry>> BuildUsbEntriesFromRegistryAsync(CancellationToken cancellationToken)
        {
            var entries = new List<UsbEntry>();

            using var baseKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(AppConstants.Registry.RegistrySystemPath);
            if (baseKey == null) return entries;

            var subKeyNames = baseKey.GetSubKeyNames();
            var controlSets = subKeyNames.Where(sk => sk.StartsWith(AppConstants.Registry.ControlSetPrefix)).ToList();

            foreach (var controlSet in controlSets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyPath = controlSet + @"\" + AppConstants.Registry.UsbEnumPath;
                using var key = baseKey.OpenSubKey(keyPath);
                if (key != null)
                {
                    var subKeyNamesList = key.GetSubKeyNames().ToList();
                    foreach (var registryKey in subKeyNamesList)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (registryKey.StartsWith(AppConstants.Registry.UsbRootPrefix)) continue;

                        var vendorId = BuildVendorId(registryKey);
                        var productId = BuildProductId(registryKey);
                        var plugged = IsPlugged(vendorId, productId);

                        using var skey = key.OpenSubKey(registryKey);
                        if (skey == null) continue;

                        var sskeyNames = skey.GetSubKeyNames().ToList();
                        foreach (var sskeyName in sskeyNames)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var serialNumber = sskeyName;
                            using var ssKey = skey.OpenSubKey(sskeyName);
                            if (ssKey == null) continue;

                            var registryValue = ssKey.GetValue("DeviceDesc")?.ToString() ?? string.Empty;
                            var deviceName = BuildDeviceName(registryValue);

                            var usbClassRegistryValue = (string[])ssKey.GetValue("CompatibleIDs") ?? Array.Empty<string>();
                            var joinStr = string.Join(" ", usbClassRegistryValue);
                            var usbClass = BuildUsbClass(joinStr);

                            var candidate = new UsbEntry(plugged, deviceName, serialNumber, vendorId, productId, usbClass);

                            if (!entries.Any(ue => ue.Equals(candidate)))
                            {
                                entries.Add(candidate);
                            }
                        }
                    }
                }
            }

            return entries;
        }

        private void ProcessUsbKeys(IEnumerable<RegistryKey> keys, List<UsbEntry> entries, CancellationToken cancellationToken)
        {
            foreach (var registryKey in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyName = registryKey.KeyName;
                if (keyName.StartsWith(AppConstants.Registry.UsbRootPrefix)) continue;

                var vendorId = BuildVendorId(keyName);
                var productId = BuildProductId(keyName);
                var plugged = IsPlugged(vendorId, productId);

                foreach (var subKey in registryKey.SubKeys)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var serialNumber = subKey.KeyName;
                    var registryValue = subKey.GetValue("DeviceDesc")?.ToString() ?? string.Empty;
                    var deviceName = BuildDeviceName(registryValue);

                    var usbClassRegistryValue = (string)subKey.GetValue("CompatibleIDs") ?? string.Empty;
                    var usbClass = BuildUsbClass(usbClassRegistryValue);

                    var lastConnected = GetLocalDateTime(
                        GetData(subKey, AppConstants.Registry.UsbDevicePropertiesGuid, AppConstants.Registry.UsbLastConnectedValue));

                    var lastRemoved = GetLocalDateTime(
                        GetData(subKey, AppConstants.Registry.UsbDevicePropertiesGuid, AppConstants.Registry.UsbLastRemovedValue));

                    var candidate = new UsbEntry(plugged, deviceName, serialNumber, vendorId, productId, usbClass, lastConnected, lastRemoved);

                    if (!entries.Any(ue => ue.Equals(candidate)))
                    {
                        entries.Add(candidate);
                    }
                }
            }
        }

        private static string BuildVendorId(string registryKey)
        {
            if (string.IsNullOrEmpty(registryKey)) return string.Empty;
            var parts = registryKey.Split('&');
            return parts.Length > 0 ? parts[0].Substring(4) : string.Empty;
        }

        private static string BuildProductId(string registryKey)
        {
            if (string.IsNullOrEmpty(registryKey)) return string.Empty;
            var parts = registryKey.Split('&');
            return parts.Length > 1 ? parts[1].Substring(4) : string.Empty;
        }

        private static string BuildDeviceName(string registryValue)
        {
            if (string.IsNullOrEmpty(registryValue)) return string.Empty;
            if (registryValue.Contains(";"))
            {
                var parts = registryValue.Split(';');
                return parts[parts.Length - 1];
            }
            return registryValue;
        }

        private static string BuildUsbClass(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var devFilter = "DevClass_";
            var classFilter = "Class_";
            var replacedValue = value.Replace("USB\\", string.Empty);

            if (replacedValue.Contains(devFilter))
            {
                var subValues = replacedValue.Split('&');
                var usbClassString = subValues.FirstOrDefault(sv => sv.Contains(devFilter))?.Replace(devFilter, string.Empty).ToLower();
                return MapUsbClass(usbClassString ?? string.Empty);
            }
            else if (replacedValue.Contains(classFilter))
            {
                var subValues = replacedValue.Split('&');
                var usbClassString = subValues.FirstOrDefault(sv => sv.Contains(classFilter))?.Replace(classFilter, string.Empty).ToLower();
                return MapUsbClass(usbClassString ?? string.Empty);
            }

            return "Unknown USB Device";
        }

        private static string MapUsbClass(string baseClass)
        {
            if (string.IsNullOrEmpty(baseClass)) return string.Empty;
            if (!AppConstants.Usb.ClassMap.TryGetValue(baseClass, out var mapped))
                return string.Empty;
            return mapped;
        }

        private static byte[] GetData(RegistryKey serialSubKey, string guidValue, string numValue)
        {
            var properties = serialSubKey.SubKeys.SingleOrDefault(t => t.KeyName == "Properties");
            if (properties == null) return null;

            var guidKey = properties.SubKeys.SingleOrDefault(t => t.KeyName == guidValue);
            if (guidKey == null) return null;

            var subKey = guidKey.SubKeys.SingleOrDefault(t => t.KeyName == numValue);
            if (subKey == null) return null;

            return subKey.Values.SingleOrDefault(t => t.ValueName == "(default)")?.ValueDataRaw;
        }

        private static DateTimeOffset? GetLocalDateTime(byte[] data)
        {
            if (data == null || data.Length != 8) return null;
            return DateTimeOffset.FromFileTime(BitConverter.ToInt64(data, 0)).ToLocalTime();
        }

        private static bool IsPlugged(string vid, string pid)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("Select * From Win32_PnPEntity");
                using var collection = searcher.Get();
                foreach (var device in collection)
                {
                    var usbDevice = Convert.ToString(device);
                    if (usbDevice.Contains(vid) && usbDevice.Contains(pid))
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}