using Activities_Inspector.Models;
using Activities_Inspector.Services.Evidence;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class EvtxFileReaderTests
    {
        private const string Logon4624 = @"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'>" +
            @"<System><Provider Name='Microsoft-Windows-Security-Auditing' Guid='{54849625-5478-4994-a5e3-3e3b0328c30d}'/>" +
            @"<EventID>4624</EventID><Version>2</Version><Level>0</Level><Task>12544</Task><Opcode>0</Opcode>" +
            @"<Keywords>0x8020000000000000</Keywords>" +
            @"<TimeCreated SystemTime='2024-01-15T08:00:00.0000000Z'/>" +
            @"<EventRecordID>100</EventRecordID><Correlation/>" +
            @"<Execution ProcessID='4' ThreadID='8'/><Channel>Security</Channel>" +
            @"<Computer>PC-1</Computer><Security/></System>" +
            @"<EventData>" +
            @"<Data Name='SubjectUserSid'>S-1-0-0</Data>" +
            @"<Data Name='SubjectUserName'>-</Data>" +
            @"<Data Name='SubjectDomainName'>-</Data>" +
            @"<Data Name='SubjectLogonId'>0x0</Data>" +
            @"<Data Name='TargetUserSid'>S-1-5-21-1</Data>" +
            @"<Data Name='TargetUserName'>mario</Data>" +
            @"<Data Name='TargetDomainName'>DOM</Data>" +
            @"<Data Name='TargetLogonId'>0xabc</Data>" +
            @"<Data Name='LogonType'>2</Data>" +
            @"<Data Name='LogonProcessName'>User32</Data>" +
            @"<Data Name='AuthenticationPackageName'>Negotiate</Data>" +
            @"<Data Name='WorkstationName'>PC-1</Data>" +
            @"<Data Name='LogonGuid'>{00000000-0000-0000-0000-000000000000}</Data>" +
            @"<Data Name='TransmittedServices'>-</Data>" +
            @"<Data Name='LmPackageName'>-</Data>" +
            @"<Data Name='KeyLength'>0</Data>" +
            @"<Data Name='ProcessId'>0x0</Data>" +
            @"<Data Name='ProcessName'>-</Data>" +
            @"<Data Name='IpAddress'>10.0.0.1</Data>" +
            @"<Data Name='IpPort'>0</Data>" +
            @"</EventData></Event>";

        private const string Shutdown6006 = @"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'>" +
            @"<System><Provider Name='EventLog'/><EventID>6006</EventID><Version>0</Version>" +
            @"<Level>4</Level><Task>0</Task><Opcode>0</Opcode><Keywords>0x80000000000000</Keywords>" +
            @"<TimeCreated SystemTime='2024-01-15T18:00:00.0000000Z'/>" +
            @"<EventRecordID>200</EventRecordID><Correlation/>" +
            @"<Execution ProcessID='4' ThreadID='8'/><Channel>System</Channel>" +
            @"<Computer>PC-1</Computer><Security/></System>" +
            @"<EventData><Data></Data></EventData></Event>";

        [Fact]
        public void Parses_4624_With_Positional_Data()
        {
            var record = EvtxFileReader.ParseEvent(Logon4624);

            Assert.Equal(4624, record.EventId);
            Assert.Equal("Microsoft-Windows-Security-Auditing", record.Source);
            Assert.Equal("PC-1", record.MachineName);
            Assert.Equal(20, record.ReplacementStrings.Length);
            Assert.Equal("mario", record.ReplacementStrings[5]);
            Assert.Equal("DOM", record.ReplacementStrings[6]);
            Assert.Equal("0xabc", record.ReplacementStrings[7]);
            Assert.Equal("2", record.ReplacementStrings[8]);
            Assert.Equal("10.0.0.1", record.ReplacementStrings[18]);
        }

        [Fact]
        public void Maps_Level_And_Task()
        {
            var shutdown = EvtxFileReader.ParseEvent(Shutdown6006);
            var logon = EvtxFileReader.ParseEvent(Logon4624);

            Assert.Equal(EventLogEntryType.Information, shutdown.EntryType);
            Assert.Equal(0, shutdown.CategoryNumber);
            Assert.Equal(EventLogEntryType.Information, logon.EntryType);
            Assert.Equal(12544, logon.CategoryNumber);
        }

        [Fact]
        public void Converts_Timestamp_To_Local()
        {
            var record = EvtxFileReader.ParseEvent(Logon4624);

            Assert.Equal(
                Activities_Inspector.Utils.DateBuilder.ToLocal(
                    new System.DateTime(2024, 1, 15, 8, 0, 0, System.DateTimeKind.Utc)),
                record.TimeGenerated);
        }
    }
}
