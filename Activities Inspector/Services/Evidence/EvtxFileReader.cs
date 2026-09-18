using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Activities_Inspector.Services.Evidence
{
    public static class EvtxFileReader
    {
        private const int EvtQueryFilePath = 0x2;
        private const int EvtQueryForwardDirection = 0x100;
        private const int EvtRenderEventXml = 1;
        private const int ErrorInsufficientBuffer = 122;
        private const int ErrorNoMoreItems = 259;

        [DllImport("wevtapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr EvtQuery(IntPtr session, string path, string query, int flags);

        [DllImport("wevtapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvtNext(IntPtr resultSet, int eventArraySize,
            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] eventArray, int timeout,
            int reserved, out int returned);

        [DllImport("wevtapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvtRender(IntPtr context, IntPtr fragment, int flags,
            int bufferSize, IntPtr buffer, out int bufferUsed, out int propertyCount);

        [DllImport("wevtapi.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvtClose(IntPtr handle);

        public static List<IEventRecord> ReadEvents(string evtxPath)
        {
            var records = new List<IEventRecord>();

            var query = EvtQuery(IntPtr.Zero, evtxPath, "*", EvtQueryFilePath | EvtQueryForwardDirection);
            if (query == IntPtr.Zero)
                throw new InvalidOperationException($"Impossibile aprire il log {evtxPath} (errore Win32 {Marshal.GetLastWin32Error()}).");

            try
            {
                var handles = new IntPtr[1];

                while (true)
                {
                    if (!EvtNext(query, 1, handles, 1000, 0, out int returned) || returned == 0)
                    {
                        var error = Marshal.GetLastWin32Error();
                        if (error != ErrorNoMoreItems && error != 0)
                            throw new InvalidOperationException($"Lettura log {evtxPath} interrotta (errore Win32 {error}).");
                        break;
                    }

                    try
                    {
                        records.Add(ParseEvent(RenderXml(handles[0])));
                    }
                    finally
                    {
                        EvtClose(handles[0]);
                    }
                }
            }
            finally
            {
                EvtClose(query);
            }

            return records;
        }

        private static string RenderXml(IntPtr eventHandle)
        {
            int bufferUsed;
            int propertyCount;
            var bufferSize = 64 * 1024;
            var buffer = Marshal.AllocHGlobal(bufferSize);

            try
            {
                while (!EvtRender(IntPtr.Zero, eventHandle, EvtRenderEventXml, bufferSize, buffer, out bufferUsed, out propertyCount))
                {
                    if (Marshal.GetLastWin32Error() != ErrorInsufficientBuffer)
                        throw new InvalidOperationException($"Rendering evento fallito (errore Win32 {Marshal.GetLastWin32Error()}).");

                    Marshal.FreeHGlobal(buffer);
                    bufferSize = Math.Max(bufferUsed, bufferSize * 2);
                    buffer = Marshal.AllocHGlobal(bufferSize);
                }

                return Marshal.PtrToStringUni(buffer, bufferUsed / 2 - 1);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        internal static IEventRecord ParseEvent(string xml)
        {
            var doc = XDocument.Parse(xml, LoadOptions.None);
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
            var system = doc.Root.Element(ns + "System");
            var eventData = doc.Root.Element(ns + "EventData");

            var data = new List<string>();
            if (eventData != null)
            {
                foreach (var item in eventData.Elements(ns + "Data"))
                    data.Add(item.Value ?? string.Empty);
            }

            var timeCreated = system.Element(ns + "TimeCreated")?.Attribute("SystemTime")?.Value;
            var timestamp = DateTime.Parse(timeCreated, null,
                System.Globalization.DateTimeStyles.RoundtripKind);

            return new FileEventRecord(
                int.Parse(system.Element(ns + "EventID")?.Value ?? "0"),
                system.Element(ns + "Provider")?.Attribute("Name")?.Value ?? string.Empty,
                DateBuilder.ToLocal(timestamp),
                system.Element(ns + "Computer")?.Value ?? string.Empty,
                data.ToArray(),
                short.TryParse(system.Element(ns + "Task")?.Value, out short task) ? task : (short)0,
                MapLevel(system.Element(ns + "Level")?.Value));
        }

        private static EventLogEntryType MapLevel(string level)
        {
            switch (level)
            {
                case "3": return EventLogEntryType.Warning;
                case "1":
                case "2": return EventLogEntryType.Error;
                default: return EventLogEntryType.Information;
            }
        }

        private sealed class FileEventRecord : IEventRecord
        {
            public FileEventRecord(int eventId, string source, DateTime timeGenerated,
                string machineName, string[] replacementStrings, short categoryNumber,
                EventLogEntryType entryType)
            {
                EventId = eventId;
                Source = source;
                TimeGenerated = timeGenerated;
                MachineName = machineName;
                ReplacementStrings = replacementStrings;
                CategoryNumber = categoryNumber;
                EntryType = entryType;
            }

            public int EventId { get; }
            public string Source { get; }
            public DateTime TimeGenerated { get; }
            public string MachineName { get; }
            public string[] ReplacementStrings { get; }
            public short CategoryNumber { get; }
            public EventLogEntryType EntryType { get; }
        }
    }
}
