using Activities_Inspector.Models;
using CSharpFunctionalExtensions;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using MigraDocCore.Rendering;
using PdfSharpCore.Utils;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Section = MigraDocCore.DocumentObjectModel.Section;
using Table = MigraDocCore.DocumentObjectModel.Tables.Table;

namespace Activities_Inspector.Services
{
    public class ReportService : IReportService
    {
        private readonly INetService _netService;

        public ReportService(INetService netService)
        {
            _netService = netService;
        }

        public async Task<Result> CreatePdfFileAsync(ReportContent content, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var document = new Document();
                var section = document.AddSection();

                SetPageProperties(document, section);

                InsertMainHeader(section);
                AddCover(content.ProvisioningType, content.Other, content.InquirerSurname, content.InquirerName,
                    content.InquirerQualification, content.ObjectDescription, section);
                AddContents(content.UsageInfos, content.InstallEntries, content.RecentFolderEntries, content.PrefetchInfoEntries, content.ShellBagEntries,
                    content.SessionEntries, content.SystemTimeChangedEntries, content.UsbEntries, section);

                var doc = FinalizeDocument(document);

                var filePath = Path.Combine(content.DestinationPath, $"Report_{DateTime.Now:dd-M-yyyy}.pdf");
                await File.WriteAllBytesAsync(filePath, doc, cancellationToken);

                return Result.Success();
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure(ex.ToString());
            }
        }

        private static void SetPageProperties(Document document, Section section)
        {
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.TopMargin = Unit.FromCentimeter(1);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1);
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1);
        }

        private static void InsertMainHeader(Section section)
        {
            if (ImageSource.ImageSourceImpl == null)
            {
                ImageSource.ImageSourceImpl = new ImageSharpImageSource<Rgba32>();
            }

            var table = section.AddTable();

            table.AddColumn(Unit.FromMillimeter(25));
            table.AddColumn(Unit.FromMillimeter(167));

            table.Borders.Top.Width = 1;
            table.Borders.Bottom.Width = 1;
            table.Borders.Left.Width = 1;
            table.Borders.Right.Width = 1;

            var row = table.AddRow();

            var cell1 = row.Cells[0];
            var image = cell1.AddImage(ImageSource.FromFile("Assets/Logo.png"));
            image.Width = Unit.FromCentimeter(2);
            image.Height = Unit.FromCentimeter(2);
            image.LockAspectRatio = true;

            var cell2 = row.Cells[1];
            var par1 = cell2.AddParagraph("ACTIVITIES INSPECTOR");
            var par2 = cell2.AddParagraph("REPORT DELLE EVIDENZE DIGITALI");
            var par3 = cell2.AddParagraph($"Documento generato in data: {DateTime.Now.ToString("dd-M-yyyy")}");

            OverrideParagraphDefaultStyle(par1, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true,
                    horizontalAlignment: ParagraphAlignment.Center);
            OverrideParagraphDefaultStyle(par2, 13, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true,
                    horizontalAlignment: ParagraphAlignment.Center);
            OverrideParagraphDefaultStyle(par3, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: false,
                    horizontalAlignment: ParagraphAlignment.Center);
        }

        private void AddCover(ProvisioningType provisioningType, string other, string inquirerSurname, string inquirerName,
            string inquirerQualification, string description, Section section)
        {
            var header = provisioningType != ProvisioningType.Other ? GetProvisioningType(provisioningType) : other;
            var headerParagraph = section.AddParagraph(header);

            OverrideParagraphDefaultStyle(headerParagraph, 13, Unit.FromMillimeter(0d), Unit.FromMillimeter(10d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true,
                    horizontalAlignment: ParagraphAlignment.Right, underline: Underline.Single);

            var obj = section.AddParagraph("OGGETTO: Relazione dei risultati prodotti dal software Activities Inspector in merito alle attivita' condotte sul PC.");
            OverrideParagraphDefaultStyle(obj, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(13d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(10d), bold: true);

            var table = section.AddTable();
            table.Borders.Width = 1;
            table.Borders.Color = Colors.Black;
            table.AddColumn(Unit.FromMillimeter(192));

            var row = table.AddRow();
            var cell = row.Cells[0];

            var inquirerSurnameLabel = cell.AddParagraph();
            inquirerSurnameLabel.AddText("Cognome investigatore: ");
            var inquirerSurnameValue = inquirerSurnameLabel.AddFormattedText(inquirerSurname ?? String.Empty);
            inquirerSurnameValue.Font.Bold = true;
            OverrideParagraphDefaultStyle(inquirerSurnameLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));

            var inquirerNameLabel = cell.AddParagraph();
            inquirerNameLabel.AddText("Nome investigatore: ");
            var inquirerNameValue = inquirerNameLabel.AddFormattedText(inquirerName ?? String.Empty);
            inquirerNameValue.Font.Bold = true;
            OverrideParagraphDefaultStyle(inquirerNameLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));

            var inquirerQualificationLabel = cell.AddParagraph();
            inquirerQualificationLabel.AddText("Qualifica investigatore: ");
            var inquirerQualificationValue = inquirerQualificationLabel.AddFormattedText(inquirerQualification ?? String.Empty);
            inquirerQualificationValue.Font.Bold = true;
            OverrideParagraphDefaultStyle(inquirerQualificationLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));

            var descriptionParagraph = section.AddParagraph("Descrizione del caso");
            OverrideParagraphDefaultStyle(descriptionParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(10d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);
            var descriptionValue = section.AddParagraph(description);
            OverrideParagraphDefaultStyle(descriptionValue, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(10d));

            AddPremise(section);
        }

        private void AddPremise(Section section)
        {
            var promiseParagraph = section.AddParagraph("Premessa");
            OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(10d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "La presente relazione e' stata prodotta attraverso il software " +
                "Activities Inspector. \n"
                + "Activities Inspector è il software che consente di estrapolare " +
                "informazioni dettagliate riguardanti l’utilizzo del PC. \n" +
                "Le informazioni elaborate coinvolgono il registro di sistema, " +
                "eventi di Windows e " +
                "file memorizzati nel file system.";
            var contentParagraph = section.AddParagraph(content);
            OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

            var table = section.AddTable();
            table.Borders.Width = 1;
            table.Borders.Color = Colors.Black;
            table.AddColumn(Unit.FromMillimeter(192));

            var row = table.AddRow();
            var cell = row.Cells[0];

            var machineName = System.Net.Dns.GetHostName();
            var privateIpAddresses = _netService.GetAvailablePrivateIPs().ToList();
            var publicIpAddress = _netService.GetPublicIPAddress();

            var machineNameLabel = cell.AddParagraph();
            machineNameLabel.AddText("Nome macchina: ");
            var machineNameValue = machineNameLabel.AddFormattedText(machineName ?? String.Empty);
            machineNameValue.Font.Bold = true;
            OverrideParagraphDefaultStyle(machineNameLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));

            var privateAddressesLabel = cell.AddParagraph();
            privateAddressesLabel.AddText("Indirizzo/i IP privati: ");
            var privateAddressesValue = privateAddressesLabel.AddFormattedText(string.Join(" ; ", privateIpAddresses) ?? String.Empty);
            privateAddressesValue.Font.Bold = true;
            OverrideParagraphDefaultStyle(privateAddressesLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));

            var publicAddressLabel = cell.AddParagraph();
            publicAddressLabel.AddText("Indirizzo IP pubblico: ");
            var publicAddressValue = publicAddressLabel.AddFormattedText(publicIpAddress ?? String.Empty);
            publicAddressValue.Font.Bold = true;
            OverrideParagraphDefaultStyle(publicAddressLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));
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

        private void AddContents(UsageInfo[] usageInfos, InstallEntry[] installedPrograms, RecentFolderEntry[] recentFolderEntries,
            PrefetchInfoEntry[] prefetchInfoEntries, ShellBagEntry[] shellBagEntries, SessionEntry[] sessionEntries,
            SystemTimeChangedEntry[] systemTimeChangedEntries, UsbEntry[] usbEntries, Section section)
        {
            AddUsageInfos(usageInfos, section);
            AddInstalledPrograms(installedPrograms, section);
            AddRecentFolderEntries(recentFolderEntries, section);
            AddPrefetchInfoEntries(prefetchInfoEntries, section);
            AddShellbagsEntries(shellBagEntries, section);
            AddSessionEntries(sessionEntries, section);
            AddSystemTimeChangedEntries(systemTimeChangedEntries, section);
            AddUsbEntries(usbEntries,section);
        }

        private void AddUsageInfos(UsageInfo[] usageInfos, Section section)
        {
            if (usageInfos == null || usageInfos.Length == 0) return;

            AddNewPage(section);

            var promiseParagraph = section.AddParagraph("Orari di accensione e spegnimento");
            OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "È la funzionalità che consente di determinare tutti gli intervalli temporali indicanti il momento \n" +
                "in cui il PC è stato acceso fino al momento in cui è stato spento. \n " +
                "Si tiene conto anche degli eventuali log indicanti i riavvii di sistema e inizio/fine della fase di standby. \n" +
                "Le date sono indicate nel formato gg/mm/aaaa e gli orari espressi attraverso lo standard GMT. \n" +
                "Vengono inoltre riportate le durate di ogni sessione ed il nome del PC su cui la rilevazione è stata effettuata.";

            var contentParagraph = section.AddParagraph(content);
            OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
                "Nome macchina"
            };

            AddHeaderToTable(table, headerLabels);

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
                    info.MachineName ?? string.Empty
                };

                AddRowValuesToTable(table, rowValues);
            }
        }

        private void AddInstalledPrograms(InstallEntry[] installedPrograms, Section section)
        {
            if (installedPrograms == null || installedPrograms.Length == 0) return;

            AddNewPage(section);

            var promiseParagraph = section.AddParagraph("Programmi installati");
            OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "La ricerca dei programmi installati viene effettuata attraverso la ricerca \n" +
                "nel registro di sistema. La ricerca analizza due percorsi specifici del registro: \n" +
                "\n" +
                "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall \n" +
                "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall \n" +
                "\n" +
                "La ricerca restituisce così tutti i software installati sia a livello di singolo utente che di macchina (tutti gli utenti).";

            var contentParagraph = section.AddParagraph(content);
            OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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

            AddHeaderToTable(table, headerLabels);

            foreach (var item in installedPrograms)
            {
                var installDate = string.Empty;

                if (item.InstallDate.HasValue)
                {
                    installDate = item.InstallDate.Value.ToShortDateString();
                }

                var rowValues = new List<string>()
                {
                    item.FileName ?? string.Empty,
                    item.DataSource ?? string.Empty,
                    item.FullPath ?? string.Empty,
                    installDate ?? string.Empty
                };

                AddRowValuesToTable(table, rowValues);
            }
        }

        private void AddRecentFolderEntries(RecentFolderEntry[] recentFolderEntries, Section section)
        {
            if (recentFolderEntries == null || recentFolderEntries.Length == 0) return;

            AddNewPage(section);

            var promiseParagraph = section.AddParagraph("File recenti");
            OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "La seguente funzionalità tiene traccia dei file aperti recentemente da un utente. \n" +
                "Ogni volta che un file viene aperto, Windows crea una sorta di collegamento allo stesso. \n" +
                "La cartella in cui vengono creati i collegamenti si trova al percorso C:\\Users\\[NOME PROFILO]\\Recent ed il software ricerca i file " +
                "con estensione .lnk contenuti in questa cartella. \n" +
                "Ogni risultato contiene il nome del file, il percorso nella cartella dei file recenti, il percorso del file originario nel file system e" +
                "la data di ultima apertura del file.";

            var contentParagraph = section.AddParagraph(content);
            OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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

            AddHeaderToTable(table, headerLabels);

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
                    actionTime ?? string.Empty
                };

                AddRowValuesToTable(table, rowValues);
            }
        }

        private void AddPrefetchInfoEntries(PrefetchInfoEntry[] prefetchInfoEntries, Section section)
        {
            if (prefetchInfoEntries == null || prefetchInfoEntries.Length == 0) return;

            AddNewPage(section);

            var promiseParagraph = section.AddParagraph("Prefetch");
            OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "I file di prefetch comunemente vengono utilizzati da Windows per velocizzare " +
                "l’esecuzione delle applicazioni. Ogni volta che un utente esegue un’applicazione (file .exe), " +
                "viene generato un file con estensione .pf rappresentante, appunto, un file prefetch. \n" +
                "Questi file vengono salvati nella cartella C:\\Windows\\Prefetch \n" +
                "All’interno della cartella Prefetch possono esserci anche dei collegamenti relativi ad applicazioni non più " +
                "installate nel PC in uso.";

            var contentParagraph = section.AddParagraph(content);
            OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
                "Data ultima esecuzione"
            };

            AddHeaderToTable(table, headerLabels);

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
                    lastRunTime ?? string.Empty
                };

                AddRowValuesToTable(table, rowValues);
            }
        }

        private void AddShellbagsEntries(ShellBagEntry[] shellBagEntries, Section section)
        {
            if (shellBagEntries == null || shellBagEntries.Length == 0) return;

            AddNewPage(section);

            var promiseParagraph = section.AddParagraph("Shellbags");
            OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
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
            OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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

            AddHeaderToTable(table, headerLabels);

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

                AddRowValuesToTable(table, rowValues);
            }
        }

        private void AddSessionEntries(SessionEntry[] sessionEntries, Section section)
        {
            if (sessionEntries == null || sessionEntries.Length == 0) return;

            AddNewPage(section);

            var promiseParagraph = section.AddParagraph("LogOn/LogOff");
            OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "Questa funzionalità ha lo scopo di determinare tutti gli accessi di un " +
                "utente ad un PC. Per accesso non si intende l’accensione del PC stesso, ma l’operazione di scelta, ed eventualmente " +
                "autenticazione, di un account. \n" +
                "La funzionalità di ricerca si basa sui log di sistema. " +
                "In particolare vengono presi come riferimento gli eventi della categoria “Sicurezza”. ";

            var contentParagraph = section.AddParagraph(content);
            OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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
                "Tipo di accesso"
            };

            AddHeaderToTable(table, headerLabels);

            foreach (var item in sessionEntries)
            {
                var logOnTime = string.Empty;
                var logOffTime = string.Empty;
                var duration = string.Empty;

                if (item.LogOnTime != null)
                {
                    logOnTime = DateBuilder.BuildFromDateTime(item.LogOnTime);
                }

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
                    item.AccessType ?? string.Empty
                };

                AddRowValuesToTable(table, rowValues);
            }
        }

        private void AddSystemTimeChangedEntries(SystemTimeChangedEntry[] systemTimeChangedEntries, Section section)
        {
            if (systemTimeChangedEntries == null || systemTimeChangedEntries.Length == 0) return;

            AddNewPage(section);

            var promiseParagraph = section.AddParagraph("Modifiche all'ora di Sistema");
            OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
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
                "Se la tabella dei risultati è vuota allora è molto probabile che non vi siano state alterazioni da parte dell’utente " +
                "o che tali log siano stati eliminati dall’utente svuotando il registro eventi. \n" +
                "La tabella dei risultati riporta il nome dell’utente che ha fatto l’eventuale modifica, " +
                "l’ora in cui è stato effettuata l’operazione, l’ora iniziale del PC prima della modifica e " +
                "l’ora del PC dopo la modifica.";

            var contentParagraph = section.AddParagraph(content);
            OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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

            AddHeaderToTable(table, headerLabels);

            foreach (var item in systemTimeChangedEntries)
            {
                var rowValues = new List<string>()
                {
                    item.AccountName ?? string.Empty,
                    item.TimeGenerated ?? string.Empty,
                    item.OldTime ?? string.Empty,
                    item.NewTime ?? string.Empty
                };

                AddRowValuesToTable(table, rowValues);
            }
        }

        private void AddUsbEntries(UsbEntry[] usbEntries, Section section)
        {
            if (usbEntries == null || usbEntries.Length == 0) return;

            AddNewPage(section);

            var promiseParagraph = section.AddParagraph("Periferiche USB");
            OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
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
            OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));

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

            AddHeaderToTable(table, headerLabels);

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

                AddRowValuesToTable(table, rowValues);
            }
        }

        private string MapUsbState(bool plugged)
            => plugged ? "Connesso" : "Non connesso";

        private static byte[] FinalizeDocument(Document document)
        {
            using var stream = new MemoryStream();
            var pdfRenderer = new PdfDocumentRenderer(true) // makes fonts available
            {
                Document = document
            };
            pdfRenderer.RenderDocument();
            pdfRenderer.PdfDocument.Save(stream);

            return stream.ToArray();
        }

        #region Utils

        private static void OverrideParagraphDefaultStyle(Paragraph paragraph, Unit size, Unit marginLeft,
            Unit spaceBefore, Unit marginRight, Unit spaceAfter, string name = "Arial",
            bool bold = false, ParagraphAlignment horizontalAlignment = ParagraphAlignment.Left, Underline underline = Underline.None)
        {
            paragraph.Format.Font.Size = size;

            paragraph.Format.LeftIndent = marginLeft;
            paragraph.Format.SpaceBefore = spaceBefore;
            paragraph.Format.RightIndent = marginRight;
            paragraph.Format.SpaceAfter = spaceAfter;

            paragraph.Format.Font.Name = name;
            paragraph.Format.Font.Bold = bold;
            paragraph.Format.Font.Underline = underline;

            paragraph.Format.Alignment = horizontalAlignment;
        }

        private static void AddSeparator(Section section)
        {
            section.AddParagraph().AddLineBreak();

            var table = section.AddTable();

            table.AddColumn(Unit.FromMillimeter(192d));

            table.Borders.Left.Width = 0;
            table.Borders.Top.Width = 1;
            table.Borders.Right.Width = 0;
            table.Borders.Bottom.Width = 0;
            table.Borders.Color = Colors.Black;

            var row = table.AddRow();
        }

        private void AddNewPage(Section section)
        {
            var pageBreak = section.AddParagraph();
            pageBreak.Format.PageBreakBefore = true;
        }

        private void AddHeaderToTable(Table table, List<string> labels)
        {
            var columnsNumber = labels.Count;
            var columnWidth = (float)192 / columnsNumber;

            for (var i=0; i<columnsNumber; i++)
            {
                table.AddColumn(Unit.FromMillimeter(columnWidth)); 
            }

            var row = table.AddRow();

            for (var i = 0; i < columnsNumber; i++)
            {
                var cell = row.Cells[i];

                var paragraph = cell.AddParagraph(labels[i]);

                OverrideParagraphDefaultStyle(paragraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true,
                    horizontalAlignment: ParagraphAlignment.Center);
            }
        }

        private void AddRowValuesToTable(Table table, List<string> values)
        {
            var row = table.AddRow();

            for (var i = 0; i < values.Count; i++)
            {
                var cell = row.Cells[i];

                var safeText = AddWordBreaks(values[i]);

                var paragraph = cell.AddParagraph(safeText);
                paragraph.Format.Alignment = ParagraphAlignment.Left;
                paragraph.Format.SpaceBefore = 0;
                paragraph.Format.SpaceAfter = 0;
                cell.Format.LeftIndent = 0; // Il contenuto della cella non ha margini a sx
            }
        }

        private string AddWordBreaks(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            const int maxWordLength = 10;
            var result = new StringBuilder();

            int count = 0;
            foreach (char c in text)
            {
                result.Append(c);
                count++;

                // Ogni tot caratteri, inserisce un punto di interruzione
                if (count >= maxWordLength && char.IsLetterOrDigit(c))
                {
                    result.Append("\u200B"); // Zero-width space (permette il word wrap)
                    count = 0;
                }
            }

            return result.ToString();
        }

        #endregion
    }
}
