using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using System;
using System.Collections.Generic;
using Table = MigraDocCore.DocumentObjectModel.Tables.Table;

namespace Activities_Inspector.Services.Reporting
{
    public static class ReportTablesBuilder
    {
        public static void AddContents(UsageInfo[] usageInfos, InstallEntry[] installedPrograms, RecentFolderEntry[] recentFolderEntries,
            PrefetchInfoEntry[] prefetchInfoEntries, ShellBagEntry[] shellBagEntries, SessionEntry[] sessionEntries,
            SystemTimeChangedEntry[] systemTimeChangedEntries, UsbEntry[] usbEntries, Section section,
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, bool shellBagsPartial = false,
            IntegrityRecord[] integrityManifest = null)
        {
            AddUsageInfos(usageInfos, section, totalWidthMm);
            AddInstalledPrograms(installedPrograms, section, totalWidthMm);
            AddRecentFolderEntries(recentFolderEntries, section, totalWidthMm);
            AddPrefetchInfoEntries(prefetchInfoEntries, section, totalWidthMm);
            AddShellbagsEntries(shellBagEntries, section, totalWidthMm, shellBagsPartial);
            AddSessionEntries(sessionEntries, section, totalWidthMm);
            AddSystemTimeChangedEntries(systemTimeChangedEntries, section, totalWidthMm);
            AddUsbEntries(usbEntries, section, totalWidthMm);
            AddIntegrityManifest(integrityManifest ?? new IntegrityRecord[0], section, totalWidthMm);
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
                "l'impronta SHA-256, la dimensione in byte e l'istante di acquisizione (UTC). \n" +
                "Le impronte sono calcolate in sola lettura al momento dell'analisi e attestano cio' che il software " +
                "ha letto in quell'istante, non l'immutabilita' del sistema: su macchina live i file possono cambiare " +
                "dopo l'acquisizione. \n" +
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
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.UsageTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.UsageKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "È la funzionalità che consente di determinare tutti gli intervalli temporali indicanti il momento \n" +
                "in cui il PC è stato acceso fino al momento in cui è stato spento. \n " +
                "Si tiene conto anche degli eventuali log indicanti i riavvii di sistema e inizio/fine della fase di standby. \n" +
                "Le date sono indicate nel formato gg/mm/aaaa e gli orari espressi attraverso lo standard GMT. \n" +
                "Vengono inoltre riportate le durate di ogni sessione ed il nome del PC su cui la rilevazione è stata effettuata.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.InstalledTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.InstalledKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "La ricerca dei programmi installati viene effettuata attraverso la ricerca \n" +
                "nel registro di sistema. La ricerca analizza due percorsi specifici del registro: \n" +
                "\n" +
                "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall \n" +
                "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall \n" +
                "\n" +
                "La ricerca restituisce così tutti i software installati sia a livello di singolo utente che di macchina (tutti gli utenti).";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.RecentsTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.RecentsKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "La seguente funzionalità tiene traccia dei file aperti recentemente da un utente. \n" +
                "Ogni volta che un file viene aperto, Windows crea una sorta di collegamento allo stesso. \n" +
                "La cartella in cui vengono creati i collegamenti si trova al percorso C:\\Users\\[NOME PROFILO]\\Recent ed il software ricerca i file " +
                "con estensione .lnk contenuti in questa cartella. \n" +
                "Ogni risultato contiene il nome del file, il percorso nella cartella dei file recenti, il percorso del file originario nel file system e" +
                "la data di ultima apertura del file.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.PrefetchTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.PrefetchKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "I file di prefetch comunemente vengono utilizzati da Windows per velocizzare " +
                "l’esecuzione delle applicazioni. Ogni volta che un utente esegue un’applicazione (file .exe), " +
                "viene generato un file con estensione .pf rappresentante, appunto, un file prefetch. \n" +
                "Questi file vengono salvati nella cartella C:\\Windows\\Prefetch \n" +
                "All’interno della cartella Prefetch possono esserci anche dei collegamenti relativi ad applicazioni non più " +
                "installate nel PC in uso.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters, bool isPartial = false)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.ShellbagsTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.ShellbagsKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "Ogni volta che viene aperta una cartella attraverso la funzione “Esplora risorse”, " +
                "Windows salva le impostazioni di questa directory nel registro di sistema. \n" +
                "Lo scopo di questa funzionalità è quello di conoscere i percorsi, nomi e data " +
                "di apertura delle cartelle aperte sia sul disco fisso che su dispositivi USB. \n" +
                "E’ importante analizzare queste chiavi in quanto siamo in grado anche di rilevare eventuali " +
                "azioni di un utente malevolo anche quando questo ha cancellato i file e le cartelle da esso visitate. \n" +
                "I risultati restituiti contengono il percorso della cartella, data di accesso, data di creazione, data " +
                "dell’ultima scrittura e il percorso nel registro di sistema.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.SessionsTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.SessionsKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "Questa funzionalità ha lo scopo di determinare tutti gli accessi di un " +
                "utente ad un PC. Per accesso non si intende l’accensione del PC stesso, ma l’operazione di scelta, ed eventualmente " +
                "autenticazione, di un account. \n" +
                "La funzionalità di ricerca si basa sui log di sistema. " +
                "In particolare vengono presi come riferimento gli eventi della categoria “Sicurezza”. ";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.TimeChangedTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.TimeChangedKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "All’avvio dell’applicazione il software verifica che l’ora e la " +
                "data del sistema siano genuine comunicando eventuali manomissioni da parte dell’utente. \n" +
                "Questa operazione è molto importante perché può farci capire se anche i log possono aver subito delle alterazioni " +
                "riportando dei dati non veritieri. \n" +
                "La verifica delle modifiche a ora e data viene effettuata ricavando l’ora esatta del sistema e confrontando la " +
                "stessa con l’ora e data restituita dal server NTP di Windows (time.windows.com). \n" +
                "nel momento in cui la discrepanza fra i due orari è maggiore di 1 minuto il software comunica con una possibile manomissione.\n" +
                "Il messaggio in questione può comparire anche nel momento in cui non è possibile interrogare il server di riferimento " +
                "perchè il PC non è connesso alla rete oppure il server non è raggiungibile. \n" +
                "Se non viene riportato alcun elemento allora e' molto probabile che non vi siano state alterazioni da parte dell'utente " +
                "o che tali log siano stati eliminati dall’utente svuotando il registro eventi. \n" +
                "La tabella dei risultati riporta il nome dell’utente che ha fatto l’eventuale modifica, " +
                "l’ora in cui è stato effettuata l’operazione, l’ora iniziale del PC prima della modifica e " +
                "l’ora del PC dopo la modifica.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
            double totalWidthMm = ReportFormatting.PortraitContentWidthMillimeters)
        {
            ReportFormatting.AddNewPage(section);

            var promiseParagraph = section.AddParagraph(ReportSectionCatalog.UsbTitle);
            promiseParagraph.AddBookmark(ReportSectionCatalog.UsbKey);
            promiseParagraph.Format.OutlineLevel = OutlineLevel.Level1;
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "La seguente funzionalità riporta tutte le periferiche USB che sono state connesse/rimosse e/dal PC in questione. \n" +
                "La ricerca dei dispositivi avviene scansionando il registro di sistema a partire dal file in C:\\Windows\\System32\\config\\SYSTEM ed intercettando" +
                " gli eventi di connessione / disconnessine. \n" +
                "I dati recuperati dal registro riguardano il nome del dispositivo, il seriale (ove disponibile), VendorId, " +
                "ProductId, classe(tipologia di dispositivo) e le date di ultimo inserimento e ultima rimozione. \n" +
                "Per ottenere i timestamps con le date di inserimento / rimozione è indispensabile avviare il software con privilegi " +
                "di amministratore. \n" +
                "\n" +
                "Attraverso gli eventi di sistema, viene inoltre reperita un'ultima informazione riguardante lo stato del dispositivo che ci permette " +
                "di determinare quando questo è connesso o meno, al momento della rilevazione.\n" +
                "L'aggiornamento dello stato avviene in realtime.";

            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
