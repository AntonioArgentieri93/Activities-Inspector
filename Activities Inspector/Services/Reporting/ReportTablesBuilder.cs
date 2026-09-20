using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using Table = MigraDocCore.DocumentObjectModel.Tables.Table;

namespace Activities_Inspector.Services.Reporting
{
    public static class ReportTablesBuilder
    {
        public static void AddContents(UsageInfo[] usageInfos, InstallEntry[] installedPrograms, RecentFolderEntry[] recentFolderEntries,
            PrefetchInfoEntry[] prefetchInfoEntries, ShellBagEntry[] shellBagEntries, SessionEntry[] sessionEntries,
            SystemTimeChangedEntry[] systemTimeChangedEntries, UsbEntry[] usbEntries, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, bool shellBagsPartial = false,
            IntegrityRecord[] integrityManifest = null, AuditEntry[] auditTrail = null,
            string usageSource = null, string installSource = null, string recentSource = null, string prefetchSource = null,
            string shellBagsSource = null, string sessionsSource = null, string timeChangedSource = null, string usbSource = null)
        {
            AddUsageInfos(usageInfos, section, totalWidthMm, usageSource);
            AddInstalledPrograms(installedPrograms, section, totalWidthMm, installSource);
            AddRecentFolderEntries(recentFolderEntries, section, totalWidthMm, recentSource);
            AddPrefetchInfoEntries(prefetchInfoEntries, section, totalWidthMm, prefetchSource);
            AddShellbagsEntries(shellBagEntries, section, totalWidthMm, shellBagsPartial, shellBagsSource);
            AddSessionEntries(sessionEntries, section, totalWidthMm, sessionsSource);
            AddSystemTimeChangedEntries(systemTimeChangedEntries, section, totalWidthMm, timeChangedSource);
            AddUsbEntries(usbEntries, section, totalWidthMm, usbSource);
            AddIntegrityManifest(integrityManifest ?? new IntegrityRecord[0], section, totalWidthMm);
            AddAuditTrail(auditTrail ?? new AuditEntry[0], section, totalWidthMm);
        }

        private static void AddSourceNote(Section section, string source)
        {
            var text = string.IsNullOrEmpty(source) ? "Sorgente: nessuna ricerca eseguita" : $"Sorgente: {source}";
            var p = section.AddParagraph(text);
            ReportFormatting.OverrideParagraphDefaultStyle(p, 9, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(3d));
            p.Format.Font.Italic = true;
            p.Format.Font.Color = Colors.DimGray;
        }

        public static void AddAuditTrail(AuditEntry[] entries, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.AuditTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.AuditKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var chainValid = Services.AuditChain.Verify(entries);

            var content = "La tabella elenca le operazioni svolte dal software in questa sessione " +
                "(ricerche, esportazioni, generazione del report) con ora in formato gg/mm/aaaa HH:mm:ss GMT. \n" +
                "Ogni riga contiene l'impronta della precedente: la catena " +
                (chainValid ? "risulta integra." : "RISULTA ALTERATA: il diario non e' attendibile.") + " \n" +
                "Il diario vive solo in memoria e non scrive nulla sul PC in esame.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            if (entries == null || entries.Length == 0)
            {
                var note = section.AddParagraph("Nessuna operazione registrata in questa sessione.");
                ReportFormatting.OverrideParagraphDefaultStyle(note, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));
                note.Format.Font.Italic = true;
                return;
            }

            var table = section.AddTable();

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var headerLabels = new List<string>()
            {
                "N.",
                "Ora",
                "Operazione",
                "Dettaglio",
                "Impronta"
            };

            ReportFormatting.AddHeaderToTable(table, headerLabels, totalWidthMm);

            foreach (var item in entries.OrderBy(e => e.Sequence))
            {
                var rowValues = new List<string>()
                {
                    item.Sequence.ToString(),
                    DateBuilder.BuildFromDateTime(DateBuilder.ToLocal(item.TimestampUtc)),
                    item.Category.ToString(),
                    item.Detail ?? string.Empty,
                    item.Hash ?? string.Empty
                };

                ReportFormatting.AddRowValuesToTable(table, rowValues);
            }
        }

        public static void AddIntegrityManifest(IntegrityRecord[] records, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.IntegrityTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.IntegrityKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "La tabella riporta per ogni artefatto letto dal PC in esame il percorso, " +
                "l'impronta SHA-256, la dimensione in byte e l'istante di acquisizione in formato gg/mm/aaaa HH:mm:ss GMT. \n" +
                "Le impronte sono calcolate in sola lettura al momento dell'analisi e attestano cio' che il software " +
                "ha letto in quell'istante, non l'immutabilita' del sistema: su macchina live i file possono cambiare " +
                "dopo l'acquisizione, su immagine il contenuto e' statico. \n" +
                "Le righe senza impronta indicano sorgenti lette via API live (nessun file acquisibile) oppure file " +
                "non leggibili, con il motivo riportato nella colonna Stato.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            if (records == null || records.Length == 0)
            {
                var note = section.AddParagraph("Nessun artefatto acquisito: eseguire le funzionalita' prima di generare il report.");
                ReportFormatting.OverrideParagraphDefaultStyle(note, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));
                note.Format.Font.Italic = true;
                return;
            }

            var table = section.AddTable();

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var headerLabels = new List<string>()
            {
                "Funzionalita'",
                "Percorso",
                "SHA-256",
                "Byte",
                "Acquisito",
                "Stato"
            };

            ReportFormatting.AddHeaderToTable(table, headerLabels, totalWidthMm);

            foreach (var item in records)
            {
                var rowValues = new List<string>()
                {
                    MapFeatureTitle(item.Feature),
                    item.Path ?? string.Empty,
                    item.Sha256 ?? string.Empty,
                    item.SizeBytes.HasValue ? item.SizeBytes.Value.ToString() : string.Empty,
                    item.AcquiredUtc.HasValue ? DateBuilder.BuildFromDateTime(DateBuilder.ToLocal(item.AcquiredUtc.Value)) : string.Empty,
                    MapIntegrityStatus(item)
                };

                ReportFormatting.AddRowValuesToTable(table, rowValues);
            }
        }

        private static string MapFeatureTitle(EntryType feature)
        {
            switch (feature)
            {
                case EntryType.InstalledPrograms: return ReportSectionCatalog.InstalledTitle;
                case EntryType.Recents: return ReportSectionCatalog.RecentsTitle;
                case EntryType.Prefetch: return ReportSectionCatalog.PrefetchTitle;
                case EntryType.Usb: return ReportSectionCatalog.UsbTitle;
                case EntryType.Sessions: return ReportSectionCatalog.SessionsTitle;
                case EntryType.TimeIntervals: return ReportSectionCatalog.UsageTitle;
                case EntryType.SystemTimeChanged: return ReportSectionCatalog.TimeChangedTitle;
                default: return feature.ToString();
            }
        }

        private static string MapIntegrityStatus(IntegrityRecord item)
        {
            switch (item.Status)
            {
                case IntegrityStatus.Acquired: return "Acquisito";
                case IntegrityStatus.LiveSource: return item.Detail ?? "Sorgente live";
                case IntegrityStatus.NotAcquirable:
                    return string.IsNullOrEmpty(item.Detail) ? "Non acquisibile" : $"Non acquisibile ({item.Detail})";
                default: return item.Status.ToString();
            }
        }

        public static void AddUsageInfos(UsageInfo[] usageInfos, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, string source = null)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.UsageTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.UsageKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "Determinazione degli intervalli di alimentazione del sistema. " +
                "La funzionalita' incrocia gli eventi di avvio (ID 6005), arresto (6006), arresto anomalo (41) e standby (42) del registro System. \n" +
                "In modalita' live il registro System e' letto via API; in modalita' immagine dal file System.evtx dell'acquisizione. \n" +
                "Le date sono in formato gg/mm/aaaa HH:mm:ss con offset GMT della data; la durata e' calcolata tra accensione e spegnimento; la colonna Avvio anomalo vale Si quando l'avvio segue un arresto non regolare. \n" +
                "L'intervallo aperto (senza spegnimento) rappresenta la sessione in corso al momento dell'acquisizione.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            AddSourceNote(section, source);

            if (usageInfos == null || usageInfos.Length == 0)
            {
                ReportFormatting.AddNoResultsNote(section);
                return;
            }

            var table = section.AddTable();

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var headerLabels = new List<string>()
            {
                "Accensione",
                "Spegnimento",
                "Durata",
                "Nome macchina",
                "Avvio anomalo"
            };

            ReportFormatting.AddHeaderToTable(table, headerLabels, totalWidthMm);

            foreach (var info in usageInfos)
            {
                var endInterval = string.Empty;
                var duration = string.Empty;

                if (info.Interval.End.HasValue)
                {
                    endInterval = DateBuilder.BuildFromDateTime(info.Interval.End.Value);
                }

                if (info.Duration != null)
                {
                    duration = $"{info.Duration.Days} giorno/i - {info.Duration.Hours} ora/e - {info.Duration.Minutes} minuti - " +
                        $"{info.Duration.Seconds} secondi.";
                }

                var rowValues = new List<string>()
                {
                    DateBuilder.BuildFromDateTime(info.Interval.Start) ?? string.Empty,
                    endInterval ?? string.Empty,
                    duration ?? string.Empty,
                    info.MachineName ?? string.Empty,
                    info.Interval.StartedAfterCrash ? "Sì" : "No"
                };

                ReportFormatting.AddRowValuesToTable(table, rowValues);
            }
        }

        public static void AddInstalledPrograms(InstallEntry[] installedPrograms, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, string source = null)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.InstalledTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.InstalledKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "Elenco dei software con evidenza di installazione. Le sorgenti sono: chiavi Uninstall del registry " +
                "(HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall 64-bit, HKLM\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall 32-bit), " +
                "chiavi per-utente (HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall 64-bit, HKCU\\Software\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall 32-bit), " +
                "log Applicazione (eventi MsiInstaller 11707/1040/1042). \n" +
                "In modalita' live le chiavi sono lette via API e il log eventi via API; in modalita' immagine dagli hive SOFTWARE, NTUSER.DAT e dal file Application.evtx dell'acquisizione. \n" +
                "La sorgente riporta la chiave di registry o il percorso del log eventi; il percorso e' l'InstallLocation del registry.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            AddSourceNote(section, source);

            if (installedPrograms == null || installedPrograms.Length == 0)
            {
                ReportFormatting.AddNoResultsNote(section);
                return;
            }

            var table = section.AddTable();

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var headerLabels = new List<string>()
            {
                "Nome file",
                "Sorgente",
                "Percorso",
                "Data"
            };

            ReportFormatting.AddHeaderToTable(table, headerLabels, totalWidthMm);

            foreach (var item in installedPrograms)
            {
                var installDate = string.Empty;

                if (item.InstallDate.HasValue)
                {
                    installDate = DateBuilder.BuildFromDateTime(item.InstallDate.Value);
                }

                var rowValues = new List<string>()
                {
                    item.FileName ?? string.Empty,
                    item.DataSource ?? string.Empty,
                    item.FullPath ?? string.Empty,
                    installDate ?? string.Empty
                };

                ReportFormatting.AddRowValuesToTable(table, rowValues);
            }
        }

        public static void AddRecentFolderEntries(RecentFolderEntry[] recentFolderEntries, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, string source = null)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.RecentsTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.RecentsKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "File aperti di recente dall'utente. Windows crea per ogni apertura un collegamento .lnk. \n" +
                "In modalita' live la ricerca interessa C:\\Users\\[profilo]\\Recent dell'utente corrente; in modalita' immagine tutti i profili presenti nell'acquisizione. \n" +
                "Ogni riga riporta il nome del file, il percorso del collegamento nella cartella Recent, il percorso del file di origine e la data di ultima apertura (LastWrite del .lnk).";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            AddSourceNote(section, source);

            if (recentFolderEntries == null || recentFolderEntries.Length == 0)
            {
                ReportFormatting.AddNoResultsNote(section);
                return;
            }

            var table = section.AddTable();

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var headerLabels = new List<string>()
            {
                "Nome file",
                "Sorgente",
                "Percorso",
                "Data",
                "Elementi saltati"
            };

            ReportFormatting.AddHeaderToTable(table, headerLabels, totalWidthMm);

            foreach (var item in recentFolderEntries)
            {
                var actionTime = string.Empty;

                if (item.ActionTime != null)
                {
                    actionTime = DateBuilder.BuildFromDateTime(item.ActionTime);
                }

                var rowValues = new List<string>()
                {
                    item.FileName ?? string.Empty,
                    item.DataSource ?? string.Empty,
                    item.FullPath ?? string.Empty,
                    actionTime ?? string.Empty,
                    item.SkippedShellItems.ToString()
                };

                ReportFormatting.AddRowValuesToTable(table, rowValues);
            }
        }

        public static void AddPrefetchInfoEntries(PrefetchInfoEntry[] prefetchInfoEntries, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, string source = null)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.PrefetchTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.PrefetchKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "File Prefetch di Windows, generati all'esecuzione di un .exe per velocizzarne l'avvio. \n" +
                "In modalita' live sono letti da C:\\Windows\\Prefetch; in modalita' immagine da Windows\\Prefetch dell'acquisizione. \n" +
                "La tabella riporta il nome dell'eseguibile, il percorso del file .pf, l'estensione, la prima e l'ultima esecuzione e il conteggio. I file corrotti vengono ignorati e registrati nel manifest di integrita'.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            AddSourceNote(section, source);

            if (prefetchInfoEntries == null || prefetchInfoEntries.Length == 0)
            {
                ReportFormatting.AddNoResultsNote(section);
                return;
            }

            var table = section.AddTable();

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var headerLabels = new List<string>()
            {
                "Nome file",
                "Sorgente",
                "Estensione",
                "Data ultima esecuzione",
                "Prima esecuzione",
                "Esecuzioni"
            };

            ReportFormatting.AddHeaderToTable(table, headerLabels, totalWidthMm);

            foreach (var item in prefetchInfoEntries)
            {
                var lastRunTime = string.Empty;

                if (item != null)
                {
                    lastRunTime = DateBuilder.BuildFromDateTime(item.LastRunTime);
                }

                var rowValues = new List<string>()
                {
                    item.ExecutableFileName ?? string.Empty,
                    item.SourceFileName ?? string.Empty,
                    item.Extension ?? string.Empty,
                    lastRunTime ?? string.Empty,
                    DateBuilder.BuildFromDateTime(item.FirstRunTime) ?? string.Empty,
                    item.RunCount.ToString()
                };

                ReportFormatting.AddRowValuesToTable(table, rowValues);
            }
        }

        public static void AddShellbagsEntries(ShellBagEntry[] shellBagEntries, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, bool isPartial = false, string source = null)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.ShellbagsTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.ShellbagsKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "Percorsi di cartelle visitate in Esplora risorse, conservati nel registry. \n" +
                "In modalita' live sono letti via API; in modalita' immagine dagli hive NTUSER.DAT e UsrClass.dat. \n" +
                "La tabella riporta il percorso assoluto ricostruito, la data di ultima scrittura della chiave e il percorso nel registry.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            AddSourceNote(section, source);

            if (isPartial)
            {
                ReportFormatting.AddPartialResultsWarning(section);
            }

            if (shellBagEntries == null || shellBagEntries.Length == 0)
            {
                ReportFormatting.AddNoResultsNote(section);
                return;
            }

            var table = section.AddTable();

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var headerLabels = new List<string>()
            {
                "Percorso assoluto",
                "Data ultima scrittura",
                "Percorso nel registro"
            };

            ReportFormatting.AddHeaderToTable(table, headerLabels, totalWidthMm);

            foreach (var item in shellBagEntries)
            {
                var lastRegistryWriteData = string.Empty;

                if (item.LastRegistryWriteDate != null)
                {
                    lastRegistryWriteData = DateBuilder.BuildFromDateTime(item.LastRegistryWriteDate);
                }

                var rowValues = new List<string>()
                {
                    item.AbsolutePath ?? string.Empty,
                    lastRegistryWriteData ?? string.Empty,
                    item.RegistryPath ?? string.Empty,
                };

                ReportFormatting.AddRowValuesToTable(table, rowValues);
            }
        }

        public static void AddSessionEntries(SessionEntry[] sessionEntries, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, string source = null)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.SessionsTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.SessionsKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "Accessi a un account sul PC, distinti dall'accensione del sistema. \n" +
                "La ricerca considera gli eventi 4624 (logon) e 4647 (logoff) del registro Sicurezza, filtrando i tipi guidati da persona (2,7,9,10,11) ed escludendo account di servizio (UMFD-, DWM-). \n" +
                "In modalita' live il registro Sicurezza e' letto via API; in modalita' immagine dal file Security.evtx. \n" +
                "La colonna Note segnala i logoff senza logon corrispondente e la colonna ID sessione il LogonId; un'assenza di righe puo' indicare sia assenza di accessi sia log ruotato o inaccessibile.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            AddSourceNote(section, source);

            if (sessionEntries == null || sessionEntries.Length == 0)
            {
                ReportFormatting.AddNoResultsNote(section);
                return;
            }

            var table = section.AddTable();

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var headerLabels = new List<string>()
            {
                "Utente",
                "Dominio",
                "Nome macchina",
                "Ora di accesso",
                "Ora disconnessione",
                "Durata",
                "Indirizzo di rete",
                "Tipo di accesso",
                "Note",
                "ID sessione"
            };

            ReportFormatting.AddHeaderToTable(table, headerLabels, totalWidthMm);

            foreach (var item in sessionEntries)
            {
                var logOnTime = DateBuilder.BuildFromDateTime(item.LogOnTime);
                var logOffTime = string.Empty;
                var duration = string.Empty;

                if (item.LogOffTime != null)
                {
                    logOffTime = DateBuilder.BuildFromDateTime(item.LogOffTime.Value);
                }

                if (item.Duration != null)
                {
                    duration = $"{item.Duration.Value.Days} giorno/i - {item.Duration.Value.Hours} ora/e - {item.Duration.Value.Minutes} minuti - " +
                        $"{item.Duration.Value.Seconds} secondi.";
                }

                var rowValues = new List<string>()
                {
                    item.UserName ?? string.Empty,
                    item.Group ?? string.Empty,
                    item.MachineName ?? string.Empty,
                    logOnTime ?? string.Empty,
                    logOffTime ?? string.Empty,
                    duration ?? string.Empty,
                    item.NetworkAddress ?? string.Empty,
                    item.AccessType ?? string.Empty,
                    item.Note ?? string.Empty,
                    item.Index ?? string.Empty
                };

                ReportFormatting.AddRowValuesToTable(table, rowValues);
            }
        }

        public static void AddSystemTimeChangedEntries(SystemTimeChangedEntry[] systemTimeChangedEntries, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, string source = null)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.TimeChangedTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.TimeChangedKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "Eventi di modifica dell'ora di sistema (ID 4616 del registro Sicurezza). \n" +
                "La ricerca considera autore, ora dell'evento, ora precedente e nuova ora, escludendo l'account di servizio S-1-5-19 e il processo svchost.exe. \n" +
                "In modalita' live il registro Sicurezza e' letto via API; in modalita' immagine dal file Security.evtx. \n" +
                "Un'assenza di righe puo' indicare sia assenza di modifiche sia log ruotato o svuotato. \n" +
                "Nota: all'avvio l'applicazione esegue separatamente una verifica NTP su time.windows.com; una discrepanza superiore a un minuto o l'irraggiungibilita' del server viene segnalata a video e non compare in questa tabella.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            AddSourceNote(section, source);

            if (systemTimeChangedEntries == null || systemTimeChangedEntries.Length == 0)
            {
                ReportFormatting.AddNoResultsNote(section);
                return;
            }

            var table = section.AddTable();

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var headerLabels = new List<string>()
            {
                "Nome utente",
                "Ora evento",
                "Orario precedente",
                "Nuovo orario"
            };

            ReportFormatting.AddHeaderToTable(table, headerLabels, totalWidthMm);

            foreach (var item in systemTimeChangedEntries)
            {
                var rowValues = new List<string>()
                {
                    item.AccountName ?? string.Empty,
                    item.TimeGenerated ?? string.Empty,
                    item.OldTime ?? string.Empty,
                    item.NewTime ?? string.Empty
                };

                ReportFormatting.AddRowValuesToTable(table, rowValues);
            }
        }

        public static void AddUsbEntries(UsbEntry[] usbEntries, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, string source = null)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.UsbTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.UsbKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "Dispositivi USB connessi o rimossi dal PC. \n" +
                "In modalita' live la ricerca legge il registry di sistema e verifica lo stato via WMI; in modalita' immagine legge l'hive SYSTEM dell'acquisizione (lo stato risulta sempre Non connesso e non vi e' monitoraggio realtime). \n" +
                "La tabella riporta stato, nome, seriale, VendorId, ProductId, classe e date di ultimo inserimento e rimozione; le date richiedono privilegi di amministratore.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            AddSourceNote(section, source);

            if (usbEntries == null || usbEntries.Length == 0)
            {
                ReportFormatting.AddNoResultsNote(section);
                return;
            }

            var table = section.AddTable();

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var headerLabels = new List<string>()
            {
                "Stato",
                "Nome dispositivo",
                "Serial number",
                "VID",
                "PID",
                "Classe number",
                "Ultimo inserimento",
                "Ultima rimozione"
            };

            ReportFormatting.AddHeaderToTable(table, headerLabels, totalWidthMm);

            foreach (var item in usbEntries)
            {
                var rowValues = new List<string>()
                {
                    MapUsbState(item.Plugged) ?? string.Empty,
                    item.DeviceName ?? string.Empty,
                    item.SerialNumber ?? string.Empty,
                    item.VendorId ?? string.Empty,
                    item.ProductId ?? string.Empty,
                    item.UsbClass ?? string.Empty,
                    DateBuilder.BuildFromDateTimeOffset(item.LastConnected) ?? string.Empty,
                    DateBuilder.BuildFromDateTimeOffset(item.LastRemoved) ?? string.Empty
                };

                ReportFormatting.AddRowValuesToTable(table, rowValues);
            }
        }

        private static string MapUsbState(bool plugged)
            => plugged ? "Connesso" : "Non connesso";
    }
}
