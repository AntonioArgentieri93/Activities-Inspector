using Aspose.Pdf;
using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class ReportService : IReportService
    {
        private readonly INetService _netService;

        private static Document _document;

        public ReportService(INetService netService)
        {
            _netService = netService;
        }

        public async Task<Result> CreatePdfFile(ProvisioningType provisioningType, string other, string inquirerSurname, 
            string inquirerName, string inquirerQualification,string description,
            UsageInfo[] usageInfos, InstallEntry[] installedPrograms, RecentFolderEntry[] recentFolderEntries, 
            PrefetchInfoEntry[] prefetchInfoEntries, ShellBagEntry[] shellBagEntries, SessionEntry[] sessionEntries,
            SystemTimeChangedEntry[] systemTimeChangedEntries, UsbEntry[] usbEntries, string destinationPath)
        {
            var taskCompletionSource = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                _document = new Document();

                await Task.Run(() =>
                {
                    AddCover(provisioningType, other, inquirerSurname, inquirerName, inquirerQualification, description);
                    AddContents(usageInfos, installedPrograms, recentFolderEntries, prefetchInfoEntries, shellBagEntries, 
                        sessionEntries, systemTimeChangedEntries, usbEntries);

                    _document.Save(Path.Combine(destinationPath, "Report.pdf"));

                    taskCompletionSource.SetResult(Result.Success());
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                taskCompletionSource.SetResult(Result.Failure(ex.Message));
            }

            return taskCompletionSource.Task.Result;
        }

        private void AddCover(ProvisioningType provisioningType, string other, string inquirerSurname, string inquirerName, 
            string inquirerQualification, string description)
        {
            var page = _document.Pages.Add();

            var header = new Aspose.Pdf.Text.TextFragment();
            if(provisioningType != ProvisioningType.Other)
            {
                header = new Aspose.Pdf.Text.TextFragment(GetProvisioningType(provisioningType));
            }
            else
            {
                header = new Aspose.Pdf.Text.TextFragment(other);
            }

            header.HorizontalAlignment = HorizontalAlignment.Right;
            header.TextState.FontSize = 13;
            header.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;
            header.TextState.Underline = true;

            var obj = new Aspose.Pdf.Text.TextFragment("OGGETTO: Relazione dei risultati prodotti dal software 'Activities Inspector'" +
                " in merito alle attivita' condotte sul PC.");
            obj.HorizontalAlignment = HorizontalAlignment.Left;
            obj.TextState.FontSize = 13;
            obj.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var table = new Table();
            table.ColumnAdjustment = ColumnAdjustment.AutoFitToWindow;
            table.Border = new BorderInfo(BorderSide.All, Color.FromRgb(System.Drawing.Color.LightGray));
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            for (int i = 1; i <= 3; i++)
            {
                var row = table.Rows.Add();

                switch (i)
                {
                    case 1:
                        row.Cells.Add("Cognome investigatore: ", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                        row.Cells.Add(inquirerSurname);
                        break;
                    case 2:
                        row.Cells.Add("Nome investigatore: ", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                        row.Cells.Add(inquirerName);
                        break;
                    case 3:
                        row.Cells.Add("Qualifica investigatore: ", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                        row.Cells.Add(inquirerQualification);
                        break;
                }
            }

            var descriptionLabel = new Aspose.Pdf.Text.TextFragment("Descrizione del caso");
            descriptionLabel.HorizontalAlignment = HorizontalAlignment.Left;
            descriptionLabel.TextState.FontSize = 13;
            descriptionLabel.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var descript = new Aspose.Pdf.Text.TextFragment(description);
            descript.HorizontalAlignment = HorizontalAlignment.Left;
            descript.TextState.FontSize = 11;

            page.Paragraphs.Add(header);
            page.Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            page.Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            page.Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            page.Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            page.Paragraphs.Add(obj);
            page.Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            page.Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            page.Paragraphs.Add(table);
            page.Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            page.Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            page.Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            page.Paragraphs.Add(descriptionLabel);
            page.Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            page.Paragraphs.Add(descript);

            AddPremise();
        }

        private void AddPremise()
        {
            _document.Pages[1].Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages[1].Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages[1].Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));

            var machineName = System.Net.Dns.GetHostName();
            var privateIpAddresses = _netService.GetAvailablePrivateIPs().ToList();
            var publicIpAddress = _netService.GetPublicIPAddress();

            var header = new Aspose.Pdf.Text.TextFragment("Premessa");
            header.HorizontalAlignment = HorizontalAlignment.Left;
            header.TextState.FontSize = 13;
            header.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var content = new Aspose.Pdf.Text.TextFragment("La presente relazione e' stata prodotta attraverso il software " +
                "Activities Inspector. \n"
                + "Activities Inspector è il software che consente di estrapolare " +
                "informazioni dettagliate riguardanti l’utilizzo del PC. \n" +
                "Le informazioni elaborate coinvolgono il registro di sistema, " +
                "eventi di Windows e " +
                "file memorizzati in cartelle specifiche del file system.");
            content.HorizontalAlignment = HorizontalAlignment.Left;
            content.TextState.FontSize = 12;

            var table = new Table();
            if (privateIpAddresses.Count <= 2)
            {
                table.ColumnWidths = "135";
            }
            else if (privateIpAddresses.Count == 3)
            {
                table.ColumnWidths = "110";
            }
            table.Border = new BorderInfo(BorderSide.All, Color.FromRgb(System.Drawing.Color.LightGray));
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            for (int i = 1; i <= 3; i++)
            {
                var row = table.Rows.Add();

                switch (i)
                {
                    case 1:
                        
                        row.Cells.Add("Nome macchina: ", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                        row.Cells.Add(machineName);
                        break;
                    case 2:
                        row.Cells.Add("Indirizzo/i IP privati: ", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                        foreach (var address in privateIpAddresses)
                        {
                            row.Cells.Add(address);
                        }
                        break;
                    case 3:
                        row.Cells.Add("Indirizzo IP pubblico: ", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                        row.Cells.Add(publicIpAddress);
                        break;
                }
            }

            _document.Pages[1].Paragraphs.Add(header);
            _document.Pages[1].Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages[1].Paragraphs.Add(content);
            _document.Pages[1].Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages[1].Paragraphs.Add(table);
        }

        private void AddContents(UsageInfo[] usageInfos, InstallEntry[] installedPrograms, RecentFolderEntry[] recentFolderEntries,
            PrefetchInfoEntry[] prefetchInfoEntries, ShellBagEntry[] shellBagEntries, SessionEntry[] sessionEntries,
            SystemTimeChangedEntry[] systemTimeChangedEntries, UsbEntry[] usbEntries)
        {
            AddUsageInfos(usageInfos);
            AddInstalledPrograms(installedPrograms);
            AddRecentFolderEntries(recentFolderEntries);
            AddPrefetchInfoEntries(prefetchInfoEntries);
            AddShellbagsEntries(shellBagEntries);
            AddSessionEntries(sessionEntries);
            AddSystemTimeChangedEntries(systemTimeChangedEntries);
            AddUsbEntries(usbEntries);
        }

        private void AddUsageInfos(UsageInfo[] usageInfos)
        {
            if (usageInfos == null || usageInfos.Length == 0) return;

            var page = _document.Pages.Add();

            var header = new Aspose.Pdf.Text.TextFragment("Orari di accensione e spegnimento");
            header.HorizontalAlignment = HorizontalAlignment.Left;
            header.TextState.FontSize = 13;
            header.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var content = new Aspose.Pdf.Text.TextFragment("È la funzionalità che consente di determinare tutti gli intervalli temporali indicanti il momento \n" +
                "in cui il PC è stato acceso fino al momento in cui è stato spento. \n " +
                "Si tiene conto anche degli eventuali log indicanti i riavvii di sistema e inizio/fine della fase di standby. \n" +
                "Le date sono indicate nel formato gg/mm/aaaa e gli orari espressi attraverso lo standard GMT. \n" +
                "Vengono inoltre riportate le durate di ogni sessione ed il nome del PC su cui la rilevazione è stata effettuata.");
            content.HorizontalAlignment = HorizontalAlignment.Left;
            content.TextState.FontSize = 12;

            int elementIndex = 0;

            var table = new Table();
            table.ColumnAdjustment = ColumnAdjustment.AutoFitToWindow;
            table.Border = new BorderInfo(BorderSide.All, Color.FromRgb(System.Drawing.Color.LightGray));
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            for (int i = 1; i <= usageInfos.Length + 1; i++)
            {
                var row = table.Rows.Add();

                if (i == 1)
                {
                    row.Cells.Add("Accensione", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Spegnimento", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Durata", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Nome macchina", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                }
                else
                {
                    var endInterval = string.Empty;
                    var duration = string.Empty;

                    if (usageInfos[elementIndex].Interval.End.HasValue)
                    {
                        endInterval = DateBuilder.BuildFromDateTime(usageInfos[elementIndex].Interval.End.Value);
                    }

                    if(usageInfos[elementIndex].Duration != null)
                    {
                        duration = $"{usageInfos[elementIndex].Duration.Days} giorno/i - {usageInfos[elementIndex].Duration.Hours} ora/e - {usageInfos[elementIndex].Duration.Minutes} minuti - " +
                            $"{usageInfos[elementIndex].Duration.Seconds} secondi.";
                    }

                    row.Cells.Add(DateBuilder.BuildFromDateTime(usageInfos[elementIndex].Interval.Start));
                    row.Cells.Add(endInterval);
                    row.Cells.Add(duration);
                    row.Cells.Add(usageInfos[elementIndex].MachineName);

                    elementIndex += 1;
                }
            }

            _document.Pages.Last().Paragraphs.Add(header);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(content);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(table);
        }

        private void AddInstalledPrograms(InstallEntry[] installedPrograms)
        {
            if (installedPrograms == null || installedPrograms.Length == 0) return;

            if(_document.Pages.Count == 1)
            {
                var page = _document.Pages.Add();
            }

            var header = new Aspose.Pdf.Text.TextFragment("Programmi installati");
            header.HorizontalAlignment = HorizontalAlignment.Left;
            header.TextState.FontSize = 13;
            header.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var content = new Aspose.Pdf.Text.TextFragment("La ricerca dei programmi installati viene effettuata attraverso la ricerca \n" +
                "nel registro di sistema. La ricerca analizza due percorsi specifici del registro: \n" +
                "\n" +
                "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall \n" +
                "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall \n" +
                "\n" +
                "La ricerca restituisce così tutti i software installati sia a livello di singolo utente che di macchina (tutti gli utenti).");
            content.HorizontalAlignment = HorizontalAlignment.Left;
            content.TextState.FontSize = 12;

            var table = new Table();
            table.ColumnAdjustment = ColumnAdjustment.AutoFitToWindow;
            table.Border = new BorderInfo(BorderSide.All, Color.FromRgb(System.Drawing.Color.LightGray));
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            int elementIndex = 0;

            for (int i = 1; i <= installedPrograms.Length + 1; i++)
            {
                var row = table.Rows.Add();

                if (i == 1)
                {
                    row.Cells.Add("Nome file", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Sorgente", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Percorso", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Data", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                }
                else
                {
                    var installDate = string.Empty;

                    if (installedPrograms[elementIndex].InstallDate.HasValue)
                    {
                        installDate = installedPrograms[elementIndex].InstallDate.Value.ToShortDateString();
                    }

                    row.Cells.Add(installedPrograms[elementIndex].FileName ?? string.Empty);
                    row.Cells.Add(installedPrograms[elementIndex].DataSource);
                    row.Cells.Add(installedPrograms[elementIndex].FullPath ?? string.Empty);
                    row.Cells.Add(installDate);

                    elementIndex += 1;
                }
            }

            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(header);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(content);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(table);
        }

        private void AddRecentFolderEntries(RecentFolderEntry[] recentFolderEntries)
        {
            if (recentFolderEntries == null || recentFolderEntries.Length == 0) return;

            if (_document.Pages.Count == 1)
            {
                var page = _document.Pages.Add();
            }

            var header = new Aspose.Pdf.Text.TextFragment("File recenti");
            header.HorizontalAlignment = HorizontalAlignment.Left;
            header.TextState.FontSize = 13;
            header.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var content = new Aspose.Pdf.Text.TextFragment("La seguente funzionalità tiene traccia dei file aperti recentemente da un utente. \n" +
                "Ogni volta che un file viene aperto, Windows crea una sorta di collegamento allo stesso. \n" +
                "La cartella in cui vengono creati i collegamenti si trova al percorso C:\\Users\\[NOME PROFILO]\\Recent ed il software ricerca i file " +
                "con estensione .lnk contenuti in questa cartella. \n" +
                "Ogni risultato contiene il nome del file, il percorso nella cartella dei file recenti, il percorso del file originario nel file system e" +
                "la data di ultima apertura del file.");
            content.HorizontalAlignment = HorizontalAlignment.Left;
            content.TextState.FontSize = 12;

            var table = new Table();
            table.ColumnAdjustment = ColumnAdjustment.AutoFitToWindow;
            table.Border = new BorderInfo(BorderSide.All, Color.FromRgb(System.Drawing.Color.LightGray));
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            int elementIndex = 0;

            for (int i = 1; i <= recentFolderEntries.Length + 1; i++)
            {
                var row = table.Rows.Add();

                if (i == 1)
                {
                    row.Cells.Add("Nome file", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Sorgente", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Percorso", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Data", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                }
                else
                {
                    var actionTime = string.Empty;

                    if(recentFolderEntries[elementIndex].ActionTime != null)
                    {
                        actionTime = DateBuilder.BuildFromDateTime(recentFolderEntries[elementIndex].ActionTime);
                    }

                    row.Cells.Add(recentFolderEntries[elementIndex].FileName);
                    row.Cells.Add(recentFolderEntries[elementIndex].DataSource);
                    row.Cells.Add(recentFolderEntries[elementIndex].FullPath);
                    row.Cells.Add(actionTime);

                    elementIndex += 1;
                }
            }

            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(header);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(content);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(table);
        }

        private void AddPrefetchInfoEntries(PrefetchInfoEntry[] prefetchInfoEntries)
        {
            if (prefetchInfoEntries == null || prefetchInfoEntries.Length == 0) return;

            if (_document.Pages.Count == 1)
            {
                var page = _document.Pages.Add();
            }

            var header = new Aspose.Pdf.Text.TextFragment("Prefetch");
            header.HorizontalAlignment = HorizontalAlignment.Left;
            header.TextState.FontSize = 13;
            header.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var content = new Aspose.Pdf.Text.TextFragment("I file di prefetch comunemente vengono utilizzati da Windows per velocizzare " +
                "l’esecuzione delle applicazioni. Ogni volta che un utente esegue un’applicazione (file .exe), " +
                "viene generato un file con estensione .pf rappresentante, appunto, un file prefetch. \n" +
                "Questi file vengono salvati nella cartella C:\\Windows\\Prefetch \n" +
                "All’interno della cartella Prefetch possono esserci anche dei collegamenti relativi ad applicazioni non più " +
                "installate nel PC in uso.");
            content.HorizontalAlignment = HorizontalAlignment.Left;
            content.TextState.FontSize = 12;

            var table = new Table();
            table.ColumnAdjustment = ColumnAdjustment.AutoFitToWindow;
            table.Border = new BorderInfo(BorderSide.All, Color.FromRgb(System.Drawing.Color.LightGray));
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            int elementIndex = 0;

            for (int i = 1; i <= prefetchInfoEntries.Length + 1; i++)
            {
                var row = table.Rows.Add();

                if (i == 1)
                {
                    row.Cells.Add("Nome file", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Sorgente", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Estensione", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Data ultima esecuzione", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                }
                else
                {
                    var lastRunTime = string.Empty;

                    if(prefetchInfoEntries[elementIndex].LastRunTime != null)
                    {
                        lastRunTime = DateBuilder.BuildFromDateTime(prefetchInfoEntries[elementIndex].LastRunTime);
                    }

                    row.Cells.Add(prefetchInfoEntries[elementIndex].ExecutableFileName);
                    row.Cells.Add(prefetchInfoEntries[elementIndex].SourceFileName);
                    row.Cells.Add(prefetchInfoEntries[elementIndex].Extension ?? string.Empty);
                    row.Cells.Add(lastRunTime);

                    elementIndex += 1;
                }
            }

            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(header);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(content);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(table);
        }

        private void AddShellbagsEntries(ShellBagEntry[] shellBagEntries)
        {
            if (shellBagEntries == null || shellBagEntries.Length == 0) return;

            if (_document.Pages.Count == 1)
            {
                var page = _document.Pages.Add();
            }

            var header = new Aspose.Pdf.Text.TextFragment("Shellbags");
            header.HorizontalAlignment = HorizontalAlignment.Left;
            header.TextState.FontSize = 13;
            header.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var content = new Aspose.Pdf.Text.TextFragment("Ogni volta che viene aperta una cartella attraverso la funzione “Esplora risorse”, " +
                "Windows salva le impostazioni di questa directory nel registro di sistema. \n" +
                "Lo scopo di questa funzionalità è quello di conoscere i percorsi, nomi e data " +
                "di apertura delle cartelle aperte sia sul disco fisso che su dispositivi USB. \n" +
                "E’ importante analizzare queste chiavi in quanto siamo in grado anche di rilevare eventuali " +
                "azioni di un utente malevolo anche quando questo ha cancellato i file e le cartelle da esso visitate. \n" +
                "I risultati restituiti contengono il percorso della cartella, data di accesso, data di creazione, data " +
                "dell’ultima scrittura e il percorso nel registro di sistema.");
            content.HorizontalAlignment = HorizontalAlignment.Left;
            content.TextState.FontSize = 12;

            var table = new Table();
            table.ColumnAdjustment = ColumnAdjustment.AutoFitToWindow;
            table.Border = new BorderInfo(BorderSide.All, Color.FromRgb(System.Drawing.Color.LightGray));
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            int elementIndex = 0;

            for (int i = 1; i <= shellBagEntries.Length + 1; i++)
            {
                var row = table.Rows.Add();

                if (i == 1)
                {
                    row.Cells.Add("Percorso assoluto", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Data ultima scrittura", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Percorso nel registro", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                }
                else
                {
                    var lastRegistryWriteData = string.Empty;

                    if (shellBagEntries[elementIndex].LastRegistryWriteDate != null)
                    {
                        lastRegistryWriteData = DateBuilder.BuildFromDateTime(shellBagEntries[elementIndex].LastRegistryWriteDate);
                    }

                    row.Cells.Add(shellBagEntries[elementIndex].AbsolutePath);
                    row.Cells.Add(lastRegistryWriteData);
                    row.Cells.Add(shellBagEntries[elementIndex].RegistryPath);

                    elementIndex += 1;
                }
            }

            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(header);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(content);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(table);
        }

        private void AddSessionEntries(SessionEntry[] sessionEntries)
        {
            if (sessionEntries == null || sessionEntries.Length == 0) return;

            if (_document.Pages.Count == 1)
            {
                var page = _document.Pages.Add();
            }

            var header = new Aspose.Pdf.Text.TextFragment("LogOn/LogOff");
            header.HorizontalAlignment = HorizontalAlignment.Left;
            header.TextState.FontSize = 13;
            header.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var content = new Aspose.Pdf.Text.TextFragment("Questa funzionalità ha lo scopo di determinare tutti gli accessi di un " +
                "utente ad un PC. Per accesso non si intende l’accensione del PC stesso, ma l’operazione di scelta, ed eventualmente " +
                "autenticazione, di un account. \n" +
                "La funzionalità di ricerca si basa sui log di sistema. " +
                "In particolare vengono presi come riferimento gli eventi della categoria “Sicurezza”. ");
            content.HorizontalAlignment = HorizontalAlignment.Left;
            content.TextState.FontSize = 12;

            var table = new Table();
            table.ColumnAdjustment = ColumnAdjustment.AutoFitToWindow;
            table.Border = new BorderInfo(BorderSide.All, Color.FromRgb(System.Drawing.Color.LightGray));
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            int elementIndex = 0;

            for (int i = 1; i <= sessionEntries.Length + 1; i++)
            {
                var row = table.Rows.Add();

                if (i == 1)
                {
                    row.Cells.Add("Utente", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Dominio", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Nome macchina", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Ora di accesso", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Ora disconnessione", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Durata", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Indirizzo di rete", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Tipo di accesso", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                }
                else
                {
                    var logOnTime = string.Empty;
                    var logOffTime = string.Empty;
                    var duration = string.Empty;

                    if(sessionEntries[elementIndex].LogOnTime != null)
                    {
                        logOnTime = DateBuilder.BuildFromDateTime(sessionEntries[elementIndex].LogOnTime);
                    }

                    if (sessionEntries[elementIndex].LogOffTime != null)
                    {
                        logOffTime = DateBuilder.BuildFromDateTime(sessionEntries[elementIndex].LogOffTime.Value);
                    }

                    if (sessionEntries[elementIndex].Duration != null)
                    {
                        duration = $"{sessionEntries[elementIndex].Duration.Value.Days} giorno/i - {sessionEntries[elementIndex].Duration.Value.Hours} ora/e - {sessionEntries[elementIndex].Duration.Value.Minutes} minuti - " +
                            $"{sessionEntries[elementIndex].Duration.Value.Seconds} secondi.";
                    }

                    row.Cells.Add(sessionEntries[elementIndex].UserName ?? string.Empty);
                    row.Cells.Add(sessionEntries[elementIndex].Group ?? string.Empty);
                    row.Cells.Add(sessionEntries[elementIndex].MachineName ?? string.Empty);
                    row.Cells.Add(logOnTime);
                    row.Cells.Add(logOffTime);
                    row.Cells.Add(duration);
                    row.Cells.Add(sessionEntries[elementIndex].NetworkAddress ?? string.Empty);
                    row.Cells.Add(sessionEntries[elementIndex].AccessType ?? string.Empty);

                    elementIndex += 1;
                }
            }

            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(header);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(content);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(table);
        }

        private void AddSystemTimeChangedEntries(SystemTimeChangedEntry[] systemTimeChangedEntries)
        {
            if (systemTimeChangedEntries == null || systemTimeChangedEntries.Length == 0) return;

            if (_document.Pages.Count == 1)
            {
                var page = _document.Pages.Add();
            }

            var header = new Aspose.Pdf.Text.TextFragment("Modifiche all'ora di sistema");
            header.HorizontalAlignment = HorizontalAlignment.Left;
            header.TextState.FontSize = 13;
            header.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var content = new Aspose.Pdf.Text.TextFragment("All’avvio dell’applicazione il software verifica che l’ora e la " +
                "data del sistema siano genuine comunicando eventuali manomissioni da parte dell’utente. \n" +
                "Questa operazione è molto importante perché può farci capire se anche i log possono aver subito delle alterazioni " +
                "riportando dei dati non veritieri. \n" +
                "La verifica delle modifiche a ora e data viene effettuata ricavando l’ora esatta del sistema e confrontando la " +
                "stessa con l’ora e data restituita dal server NTP di Windows (time.windows.com). \n" +
                "nel momento in cui la discrepanza fra i due orari è maggiore di 1 minuto il software comunica con una possibile manomissione.\n" +
                "Il messaggio in questione può comparire anche nel momento in cui non è possibile interrogare il server di riferimento " +
                "perchè il PC non è connesso alla rete oppure il server non è raggiungibile. \n" +
                "Se la tabella dei risultati è vuota allora è molto probabile che non vi siano state alterazioni da parte dell’utente " +
                "o che tali log siano stati eliminati dall’utente svuotando il registro eventi. \n" +
                "La tabella dei risultati riporta il nome dell’utente che ha fatto l’eventuale modifica, " +
                "l’ora in cui è stato effettuata l’operazione, l’ora iniziale del PC prima della modifica e " +
                "l’ora del PC dopo la modifica.");
            content.HorizontalAlignment = HorizontalAlignment.Left;
            content.TextState.FontSize = 12;

            var table = new Table();
            table.ColumnAdjustment = ColumnAdjustment.AutoFitToWindow;
            table.Border = new BorderInfo(BorderSide.All, Color.FromRgb(System.Drawing.Color.LightGray));
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            int elementIndex = 0;

            for (int i = 1; i <= systemTimeChangedEntries.Length + 1; i++)
            {
                var row = table.Rows.Add();

                if (i == 1)
                {
                    row.Cells.Add("Nome utente", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Ora evento", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Orario precedente", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Nuovo orario", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                }
                else
                {
                    row.Cells.Add(systemTimeChangedEntries[elementIndex].AccountName ?? string.Empty);
                    row.Cells.Add(systemTimeChangedEntries[elementIndex].TimeGenerated ?? string.Empty);
                    row.Cells.Add(systemTimeChangedEntries[elementIndex].OldTime ?? string.Empty);
                    row.Cells.Add(systemTimeChangedEntries[elementIndex].NewTime ?? string.Empty);

                    elementIndex += 1;
                }
            }

            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(header);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(content);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(table);
        }

        private void AddUsbEntries(UsbEntry[] usbEntries)
        {
            if (usbEntries == null || usbEntries.Length == 0) return;

            if (_document.Pages.Count == 1)
            {
                var page = _document.Pages.Add();
            }

            var header = new Aspose.Pdf.Text.TextFragment("Periferiche USB");
            header.HorizontalAlignment = HorizontalAlignment.Left;
            header.TextState.FontSize = 13;
            header.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;

            var content = new Aspose.Pdf.Text.TextFragment("La seguente funzionalità riporta tutte le periferiche USB che sono state connesse/rimosse e/dal PC in questione. \n" +
                "La ricerca dei dispositivi avviene scansionando il registro di sistema a partire dal file in C:\\Windows\\System32\\config\\SYSTEM ed intercettando" +
                " gli eventi di connessione / disconnessine. \n" +
                "I dati recuperati dal registro riguardano il nome del dispositivo, il seriale (ove disponibile), VendorId, " +
                "ProductId, classe(tipologia di dispositivo) e le date di ultimo inserimento e ultima rimozione. \n" +
                "Per ottenere i timestamps con le date di inserimento / rimozione è indispensabile avviare il software con privilegi " +
                "di amministratore. \n" +
                "\n" +
                "Attraverso gli eventi di sistema, viene inoltre reperita un'ultima informazione riguardante lo stato del dispositivo che ci permette " +
                "di determinare quando questo è connesso o meno, al momento della rilevazione.\n" +
                "L'aggiornamento dello stato avviene in realtime.");
            content.HorizontalAlignment = HorizontalAlignment.Left;
            content.TextState.FontSize = 12;

            var table = new Table();
            table.ColumnAdjustment = ColumnAdjustment.AutoFitToWindow;
            table.Border = new BorderInfo(BorderSide.All, Color.FromRgb(System.Drawing.Color.LightGray));
            table.DefaultCellPadding = new MarginInfo(5, 5, 5, 5);

            int elementIndex = 0;

            for (int i = 1; i <= usbEntries.Length + 1; i++)
            {
                var row = table.Rows.Add();

                if (i == 1)
                {
                    row.Cells.Add("Stato", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Nome dispositivo", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Serial number", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("VID", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("PID", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Classe number", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Ultimo inserimento", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                    row.Cells.Add("Ultima rimozione", new Aspose.Pdf.Text.TextState { FontStyle = Aspose.Pdf.Text.FontStyles.Bold, FontSize = 11 });
                }
                else
                {
                    row.Cells.Add(MapUsbState(usbEntries[elementIndex].Plugged) ?? string.Empty);
                    row.Cells.Add(usbEntries[elementIndex].DeviceName ?? string.Empty);
                    row.Cells.Add(usbEntries[elementIndex].SerialNumber ?? string.Empty);
                    row.Cells.Add(usbEntries[elementIndex].VendorId ?? string.Empty);
                    row.Cells.Add(usbEntries[elementIndex].ProductId ?? string.Empty);
                    row.Cells.Add(usbEntries[elementIndex].UsbClass ?? string.Empty);
                    row.Cells.Add(DateBuilder.BuildFromDateTimeOffset(usbEntries[elementIndex].LastConnected) ?? string.Empty);
                    row.Cells.Add(DateBuilder.BuildFromDateTimeOffset(usbEntries[elementIndex].LastRemoved) ?? string.Empty);

                    elementIndex += 1;
                }
            }

            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(header);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(content);
            _document.Pages.Last().Paragraphs.Add(new Aspose.Pdf.Text.TextFragment("\n"));
            _document.Pages.Last().Paragraphs.Add(table);
        }

        private string GetProvisioningType(ProvisioningType provisioningType)
        {
            switch (provisioningType)
            {
                case ProvisioningType.OfficialTechnicalConsultancy:
                    return Activities_Inspector.Resources.ReportWindow_OfficialTechnicalConsultancy_Value;
                case ProvisioningType.TechnicalConsultancy:
                    return Activities_Inspector.Resources.ReportWindow_TechnicalConsultancy_Value;
                case ProvisioningType.Expertise:
                    return Activities_Inspector.Resources.ReportWindow_Expertise_Value;
                case ProvisioningType.ParereProveritate:
                    return Activities_Inspector.Resources.ReportWindow_ParereProveritate_Value;
                case ProvisioningType.Other:
                    return Activities_Inspector.Resources.ReportWindow_Other_Value;
                default:
                    throw new ArgumentException($"Valore di {nameof(provisioningType)} inaspettato.");
            }
        }

        private string MapUsbState(bool plugged)
            => plugged ? "Connesso" : "Non connesso";
    }
}
