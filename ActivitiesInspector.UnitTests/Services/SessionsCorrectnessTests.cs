using Activities_Inspector.Models;
using Activities_Inspector.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    /// <summary>Sessioni: unione dei Logon ID collegati, fine da 4647/4634, sblocco escluso, periodo coperto.</summary>
    public class SessionsCorrectnessTests
    {
        private const string Provider = "Microsoft-Windows-Security-Auditing";
        private const string Elevated = "%%1842";
        private const string Limited = "%%1843";
        private const string Account = "antonio.argentieri@hotmail.it";

        private sealed class SecEv : IEventRecord
        {
            public int EventId { get; set; }
            public string Source { get; set; } = Provider;
            public DateTime TimeGenerated { get; set; }
            public string MachineName { get; set; } = "MSI";
            public string[] ReplacementStrings { get; set; }
            public short CategoryNumber => 0;
            public EventLogEntryType EntryType => EventLogEntryType.Information;
        }

        private static DateTime T(string s) => DateTime.ParseExact(s, "yyyy-MM-dd HH:mm:ss", null);

        // 4624 v2: 2 SubjectDomain, 5 TargetUser, 6 TargetDomain, 7 LogonId, 8 LogonType, 18 Ip, 25 LinkedLogonId, 26 ElevatedToken
        private static IEventRecord Logon(string time, string id, int type, string linked = "0x0", string elevated = Elevated,
            string user = Account, string domain = "MicrosoftAccount", string ip = "127.0.0.1")
        {
            var rs = Enumerable.Repeat(string.Empty, 27).ToArray();
            rs[2] = "WORKGROUP"; rs[5] = user; rs[6] = domain; rs[7] = id; rs[8] = type.ToString(); rs[18] = ip;
            rs[25] = linked; rs[26] = elevated;
            return new SecEv { EventId = 4624, TimeGenerated = T(time), ReplacementStrings = rs };
        }

        // 4647: 0 Sid, 1 TargetUser, 2 TargetDomain, 3 LogonId
        private static IEventRecord UserLogoff(string time, string id, string user = "anton")
            => new SecEv { EventId = 4647, TimeGenerated = T(time), ReplacementStrings = new[] { "S-1-5-21", user, "MSI", id } };

        // 4634: 0 Sid, 1 TargetUser, 2 TargetDomain, 3 LogonId, 4 LogonType
        private static IEventRecord SessionEnded(string time, string id, int type = 2, string user = "anton")
            => new SecEv { EventId = 4634, TimeGenerated = T(time), ReplacementStrings = new[] { "S-1-5-21", user, "MSI", id, type.ToString() } };

        private static List<SessionEntry> Build(params IEventRecord[] events)
            => LoggedInfoService.BuildSessions(events);

        // --- Dati reali del registro Sicurezza del PC (04-06/10/2026) ---

        private static IEventRecord[] RealEvents() => new[]
        {
            // 04/10 14:36:51 - accesso (gemelli tipo 11) + due sblocchi (tipo 7) chiusi nello stesso secondo
            Logon("2026-10-04 14:36:51", "0x203925", 11, linked: "0x203997", elevated: Elevated),
            Logon("2026-10-04 14:36:51", "0x203997", 11, linked: "0x203925", elevated: Limited),
            Logon("2026-10-04 14:36:51", "0x20418e", 7, linked: "0x2041f0", elevated: Elevated, ip: "-"),
            Logon("2026-10-04 14:36:51", "0x2041f0", 7, linked: "0x20418e", elevated: Limited, ip: "-"),
            SessionEnded("2026-10-04 14:36:51", "0x2041f0", 7),
            SessionEnded("2026-10-04 14:36:51", "0x20418e", 7),
            UserLogoff("2026-10-04 15:37:35", "0x203997"),
            // 04/10 19:01:23
            Logon("2026-10-04 19:01:23", "0x2d7256f", 11, linked: "0x2d725be", elevated: Elevated),
            Logon("2026-10-04 19:01:23", "0x2d725be", 11, linked: "0x2d7256f", elevated: Limited),
            Logon("2026-10-04 19:01:23", "0x2d72bce", 7, linked: "0x2d72c6a", elevated: Elevated, ip: "-"),
            Logon("2026-10-04 19:01:23", "0x2d72c6a", 7, linked: "0x2d72bce", elevated: Limited, ip: "-"),
            SessionEnded("2026-10-04 19:01:23", "0x2d72c6a", 7),
            SessionEnded("2026-10-04 19:01:23", "0x2d72bce", 7),
            UserLogoff("2026-10-04 19:43:45", "0x2d725be"),
            // 06/10 22:03:44 - ancora aperta
            Logon("2026-10-06 22:03:44", "0x405001d", 11, linked: "0x40500a9", elevated: Elevated),
            Logon("2026-10-06 22:03:44", "0x40500a9", 11, linked: "0x405001d", elevated: Limited),
            Logon("2026-10-06 22:03:44", "0x4050ffb", 7, linked: "0x4050f33", elevated: Limited, ip: "-"),
            Logon("2026-10-06 22:03:44", "0x4050f33", 7, linked: "0x4050ffb", elevated: Elevated, ip: "-"),
            SessionEnded("2026-10-06 22:03:44", "0x4050f33", 7),
            SessionEnded("2026-10-06 22:03:44", "0x4050ffb", 7),
            // rumore: sistema, servizi, batch, finestre di sistema
            Logon("2026-10-04 14:36:52", "0x3e7", 5, user: "SYSTEM", domain: "NT AUTHORITY", ip: "-"),
            Logon("2026-10-04 14:36:52", "0x21b85c", 5, user: "MSSQLSERVER", domain: "NT Service", ip: "-"),
            Logon("2026-10-04 15:26:14", "0x25088e3", 4, user: "anton", domain: "MSI", ip: "-"),
            SessionEnded("2026-10-04 15:26:15", "0x25088e3", 4),
            SessionEnded("2026-10-04 14:37:11", "0x21b85c", 5, user: "MSSQLSERVER"),
            Logon("2026-10-04 19:43:40", "0x3e349fb", 2, user: "UMFD-3", domain: "Font Driver Host", ip: "-"),
            Logon("2026-10-04 19:43:40", "0x3e356a5", 2, user: "DWM-3", domain: "Window Manager", ip: "-"),
        };

        [Fact]
        public void Real_Data_Yields_Three_Sessions_Instead_Of_Twelve_Rows()
        {
            var sessions = Build(RealEvents());

            // Prima: 12 righe (gemelli doppi + sblocchi), 10 senza uscita. Ora: i 3 accessi veri.
            Assert.Equal(3, sessions.Count);

            Assert.Equal(T("2026-10-04 14:36:51"), sessions[0].LogOnTime);
            Assert.Equal(T("2026-10-04 15:37:35"), sessions[0].LogOffTime);
            Assert.Equal(TimeSpan.FromMinutes(60) + TimeSpan.FromSeconds(44), sessions[0].Duration);

            Assert.Equal(T("2026-10-04 19:01:23"), sessions[1].LogOnTime);
            Assert.Equal(T("2026-10-04 19:43:45"), sessions[1].LogOffTime);

            Assert.Equal(T("2026-10-06 22:03:44"), sessions[2].LogOnTime);
            Assert.Null(sessions[2].LogOffTime);
            Assert.Null(sessions[2].Duration);

            Assert.All(sessions, s => Assert.Equal("11", s.AccessType));
        }

        [Fact]
        public void Linked_Twins_Are_Merged_With_The_Limited_Session_As_Primary()
        {
            var sessions = Build(RealEvents());

            Assert.Equal("0x203997 (collegato 0x203925)", sessions[0].Index);    // il gemello non elevato riceve il 4647
            Assert.Equal("0x2d725be (collegato 0x2d7256f)", sessions[1].Index);
            Assert.Equal("0x40500a9 (collegato 0x405001d)", sessions[2].Index);
        }

        [Fact]
        public void Unlock_Type_7_Is_Not_A_Session()
        {
            var sessions = Build(
                Logon("2026-10-04 14:36:51", "0x20418e", 7, linked: "0x2041f0", elevated: Elevated, ip: "-"),
                Logon("2026-10-04 14:36:51", "0x2041f0", 7, linked: "0x20418e", elevated: Limited, ip: "-"));

            Assert.Empty(sessions);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        public void Human_Access_Types_Are_Sessions(int type)
        {
            var session = Assert.Single(Build(Logon("2026-10-04 10:00:00", "0x100", type)));

            Assert.Equal(type.ToString(), session.AccessType);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(8)]
        public void Network_Batch_Service_And_Unlock_Are_Not_Sessions(int type)
        {
            Assert.Empty(Build(Logon("2026-10-04 10:00:00", "0x100", type)));
        }

        [Fact]
        public void System_Window_And_Font_Driver_Accounts_Are_Excluded()
        {
            Assert.Empty(Build(
                Logon("2026-10-04 19:43:40", "0x1", 2, user: "UMFD-3", domain: "Font Driver Host"),
                Logon("2026-10-04 19:43:40", "0x2", 2, user: "DWM-3", domain: "Window Manager")));
        }

        // --- Fine della sessione ---

        [Fact]
        public void Logoff_On_Either_Twin_Closes_The_Merged_Session()
        {
            var events = new[]
            {
                Logon("2026-10-04 10:00:00", "0xA", 2, linked: "0xB", elevated: Elevated),
                Logon("2026-10-04 10:00:00", "0xB", 2, linked: "0xA", elevated: Limited),
            };

            var onLimited = Assert.Single(Build(events.Concat(new[] { UserLogoff("2026-10-04 12:00:00", "0xB") }).ToArray()));
            var onElevated = Assert.Single(Build(events.Concat(new[] { UserLogoff("2026-10-04 12:00:00", "0xA") }).ToArray()));

            Assert.Equal(T("2026-10-04 12:00:00"), onLimited.LogOffTime);
            Assert.Equal(T("2026-10-04 12:00:00"), onElevated.LogOffTime);
        }

        [Fact]
        public void SessionEnded_4634_Closes_When_There_Is_No_4647()
        {
            var session = Assert.Single(Build(
                Logon("2026-10-04 10:00:00", "0xA", 10),
                SessionEnded("2026-10-04 11:30:00", "0xA", 10)));

            Assert.Equal(T("2026-10-04 11:30:00"), session.LogOffTime);
            Assert.Equal(TimeSpan.FromMinutes(90), session.Duration);
        }

        [Fact]
        public void UserInitiated_4647_Is_Preferred_And_The_Later_4634_Creates_No_Extra_Row()
        {
            var session = Assert.Single(Build(
                Logon("2026-10-04 10:00:00", "0xA", 2),
                UserLogoff("2026-10-04 12:00:00", "0xA"),
                SessionEnded("2026-10-04 12:00:01", "0xA", 2)));

            Assert.Equal(T("2026-10-04 12:00:00"), session.LogOffTime);
            Assert.Null(session.Note);
        }

        [Fact]
        public void SessionEnded_Without_Logon_Creates_No_Row()
        {
            // Migliaia di 4634 riguardano servizi, batch e rete: non sono sessioni da elencare
            Assert.Empty(Build(SessionEnded("2026-10-04 12:00:00", "0xFFF", 5, user: "MSSQLSERVER")));
        }

        [Fact]
        public void UserLogoff_Without_Logon_Creates_A_Note_Row()
        {
            var row = Assert.Single(Build(UserLogoff("2026-10-04 12:00:00", "0xFFF")));

            Assert.Equal("Accesso non trovato nel log", row.Note);
            Assert.Equal(DateTime.MinValue, row.LogOnTime);
            Assert.Equal(T("2026-10-04 12:00:00"), row.LogOffTime);
        }

        // --- Logon ID che si ripetono tra un avvio e l'altro ---

        [Fact]
        public void Reused_Logon_Id_Pairs_With_The_Closest_Preceding_Logon()
        {
            // Stesso Logon ID in due avvii diversi: l'uscita del secondo non deve chiudere il primo
            var sessions = Build(
                Logon("2026-09-01 08:00:00", "0xABC", 2),
                Logon("2026-10-04 09:00:00", "0xABC", 2),
                UserLogoff("2026-10-04 10:00:00", "0xABC"));

            Assert.Equal(2, sessions.Count);
            Assert.Null(sessions[0].LogOffTime);                                    // quella di settembre resta aperta
            Assert.Equal(T("2026-10-04 10:00:00"), sessions[1].LogOffTime);
        }

        [Fact]
        public void Same_Logon_Id_At_Different_Times_Is_Not_Dropped_As_A_Duplicate()
        {
            var sessions = Build(
                Logon("2026-09-01 08:00:00", "0xABC", 2),
                Logon("2026-10-04 09:00:00", "0xABC", 2));

            Assert.Equal(2, sessions.Count);
        }

        [Fact]
        public void Identical_Event_Read_Twice_Is_Deduplicated()
        {
            var logon = Logon("2026-10-04 09:00:00", "0xABC", 2);

            Assert.Single(Build(logon, logon));
        }

        [Fact]
        public void Zero_Linked_Id_Is_Not_Treated_As_A_Twin()
        {
            var sessions = Build(
                Logon("2026-10-04 09:00:00", "0xA1", 2, linked: "0x0"),
                Logon("2026-10-04 09:00:00", "0xA2", 2, linked: "0x0"));

            Assert.Equal(2, sessions.Count);
            Assert.All(sessions, s => Assert.DoesNotContain("collegato", s.Index));
        }

        [Fact]
        public void Twin_Far_Apart_In_Time_Is_Not_Merged()
        {
            var sessions = Build(
                Logon("2026-10-04 09:00:00", "0xA", 2, linked: "0xB"),
                Logon("2026-10-05 09:00:00", "0xB", 2, linked: "0xA"));

            Assert.Equal(2, sessions.Count);
        }

        // --- Robustezza ---

        [Fact]
        public void Events_With_Too_Few_Fields_Do_Not_Throw()
        {
            var shortLogon = new SecEv { EventId = 4624, TimeGenerated = T("2026-10-04 09:00:00"), ReplacementStrings = new[] { "a", "b" } };
            var shortLogoff = new SecEv { EventId = 4647, TimeGenerated = T("2026-10-04 09:00:00"), ReplacementStrings = new[] { "a" } };
            var nullStrings = new SecEv { EventId = 4624, TimeGenerated = T("2026-10-04 09:00:00"), ReplacementStrings = null };

            Assert.Empty(Build(shortLogon, shortLogoff, nullStrings));
        }

        [Fact]
        public void Older_4624_Without_Linked_Fields_Still_Yields_A_Session()
        {
            var rs = Enumerable.Repeat(string.Empty, 19).ToArray();   // senza i campi 25/26 (sistemi piu' vecchi)
            rs[5] = "alice"; rs[6] = "PC"; rs[7] = "0x99"; rs[8] = "2"; rs[18] = "-";

            var session = Assert.Single(Build(new SecEv { EventId = 4624, TimeGenerated = T("2026-10-04 09:00:00"), ReplacementStrings = rs }));

            Assert.Equal("0x99", session.Index);
        }

        // --- Layout del 4624 diversi tra le build di Windows ---

        // Windows 11 build 26100: 28 campi (un campo in piu' in posizione 25), valori reali dal PC
        private static IEventRecord Win11Logon(string time, string id, string linked, string elevated)
        {
            var rs = new[]
            {
                "S-1-5-18", "MSI$", "WORKGROUP", "0x3e7", "S-1-5-21-2817601148-3543085385-2292922267-1001",
                Account, "MicrosoftAccount", id, "11", "User32", "Negotiate", "MSI", "{00000000-0000-0000-0000-000000000000}",
                "-", "-", "0", "0xa38", @"C:\Windows\System32\svchost.exe", "127.0.0.1", "0", "%%1833", "-", "-", "-", "-",
                "%%1843", linked, elevated
            };
            Assert.Equal(28, rs.Length);
            return new SecEv { EventId = 4624, TimeGenerated = T(time), ReplacementStrings = rs };
        }

        [Fact]
        public void Windows11_28_Field_Layout_Merges_The_Linked_Sessions()
        {
            // Regressione: con indici fissi (25/26) l'ID collegato risultava "%%1843" e i gemelli restavano due righe
            var sessions = Build(
                Win11Logon("2026-10-06 22:03:44", "0x40500a9", linked: "0x405001d", elevated: Limited),
                Win11Logon("2026-10-06 22:03:44", "0x405001d", linked: "0x40500a9", elevated: Elevated));

            var session = Assert.Single(sessions);
            Assert.Equal("0x40500a9 (collegato 0x405001d)", session.Index);
            Assert.Equal("11", session.AccessType);
            Assert.Equal("127.0.0.1", session.NetworkAddress);
        }

        [Fact]
        public void Windows11_Layout_Closes_The_Merged_Session_With_The_Limited_Logoff()
        {
            var session = Assert.Single(Build(
                Win11Logon("2026-10-04 19:01:23", "0x2d7256f", linked: "0x2d725be", elevated: Elevated),
                Win11Logon("2026-10-04 19:01:23", "0x2d725be", linked: "0x2d7256f", elevated: Limited),
                UserLogoff("2026-10-04 19:43:45", "0x2d725be")));

            Assert.Equal(T("2026-10-04 19:43:45"), session.LogOffTime);
            Assert.Equal(TimeSpan.FromMinutes(42) + TimeSpan.FromSeconds(22), session.Duration);
        }

        [Fact]
        public void Documented_27_Field_Layout_Still_Works()
        {
            var sessions = Build(
                Logon("2026-10-04 10:00:00", "0xA", 2, linked: "0xB", elevated: Elevated),
                Logon("2026-10-04 10:00:00", "0xB", 2, linked: "0xA", elevated: Limited));

            Assert.Equal("0xB (collegato 0xA)", Assert.Single(sessions).Index);
        }

        [Theory]
        [InlineData(27, "0x1f", "%%1842", "0x1f", "%%1842")]
        [InlineData(28, "0xABC", "%%1843", "0xABC", "%%1843")]
        [InlineData(26, "0x1f", "%%1842", null, null)]            // piu' vecchio: nessun campo
        [InlineData(28, "%%1843", "0x1f", null, null)]            // forma sbagliata (campi scambiati): ignorati
        [InlineData(28, "-", "-", null, null)]
        public void LinkedFields_Are_Read_From_The_End_And_Validated(int count, string penultimate, string last, string expectedLinked, string expectedElevated)
        {
            var values = Enumerable.Repeat("x", count).ToArray();
            values[count - 2] = penultimate;
            values[count - 1] = last;

            LoggedInfoService.ExtractLinkedFields(values, out var linked, out var elevated);

            Assert.Equal(expectedLinked, linked);
            Assert.Equal(expectedElevated, elevated);
        }

        [Fact]
        public void LinkedFields_Of_Null_Or_Short_Arrays_Are_Absent()
        {
            LoggedInfoService.ExtractLinkedFields(null, out var l1, out var e1);
            LoggedInfoService.ExtractLinkedFields(new[] { "a", "b" }, out var l2, out var e2);

            Assert.Null(l1); Assert.Null(e1); Assert.Null(l2); Assert.Null(e2);
        }

        // --- Data assente ---

        [Fact]
        public void Missing_Logon_Time_Is_Shown_As_NA_In_The_Csv()
        {
            // Regressione: l'uscita senza accesso mostrava "01/1/0001 00:00:00 GMT+1"
            var orphan = Build(UserLogoff("2026-10-04 15:37:35", "0x203997")).Single();

            var csv = new EntryFormatter().AsCsv(orphan).Split(new[] { " ; " }, StringSplitOptions.None);

            Assert.Equal("N/D", csv[3]);                      // Ora di accesso
            Assert.DoesNotContain("0001", string.Join(" ", csv));
        }

        [Fact]
        public void DateBuilder_Never_Renders_The_Min_Value_Placeholder()
        {
            Assert.Equal("N/D", Activities_Inspector.Utils.DateBuilder.BuildFromDateTime(DateTime.MinValue));
        }

        // --- Periodo coperto dal registro ---

        [Fact]
        public void Coverage_Reports_First_Last_And_Count()
        {
            var coverage = LogCoverage.FromTimes("Security", new[] { T("2026-10-04 14:36:51"), T("2026-10-06 22:39:50"), T("2026-10-05 08:00:00") });

            Assert.Equal(T("2026-10-04 14:36:51"), coverage.From);
            Assert.Equal(T("2026-10-06 22:39:50"), coverage.To);
            Assert.Equal(3, coverage.EventCount);
            Assert.Equal("Registro Security: 3 eventi dal 04/10/2026 14:36:51 al 06/10/2026 22:39:50", coverage.ToText());
        }

        [Fact]
        public void Coverage_Of_An_Empty_Log_Says_So()
        {
            Assert.Equal("Registro Security: nessun evento", LogCoverage.FromTimes("Security", new DateTime[0]).ToText());
        }
    }
}
