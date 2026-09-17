using Activities_Inspector.Models;
using Activities_Inspector.Services;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using PdfSharpCore.Utils;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Linq;
using Document = MigraDocCore.DocumentObjectModel.Document;
using Section = MigraDocCore.DocumentObjectModel.Section;
using Table = MigraDocCore.DocumentObjectModel.Tables.Table;

namespace Activities_Inspector.Services.Reporting
{
    public class ReportCoverBuilder
    {
        private readonly INetService _netService;

        public ReportCoverBuilder(INetService netService)
        {
            _netService = netService;
        }

        public void BuildCover(ReportContent content, Document document, Section section)
        {
            SetPageProperties(document, section);

            InsertMainHeader(section);
            AddCover(content.ProvisioningType, content.Other, content.InquirerSurname, content.InquirerName,
                content.InquirerQualification, content.ObjectDescription, section);
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

            ReportFormatting.OverrideParagraphDefaultStyle(par1, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true,
                    horizontalAlignment: ParagraphAlignment.Center);
            ReportFormatting.OverrideParagraphDefaultStyle(par2, 13, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true,
                    horizontalAlignment: ParagraphAlignment.Center);
            ReportFormatting.OverrideParagraphDefaultStyle(par3, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: false,
                    horizontalAlignment: ParagraphAlignment.Center);
        }

        private void AddCover(ProvisioningType provisioningType, string other, string inquirerSurname, string inquirerName,
            string inquirerQualification, string description, Section section)
        {
            var header = provisioningType != ProvisioningType.Other ? GetProvisioningType(provisioningType) : other;
            var headerParagraph = section.AddParagraph(header);

            ReportFormatting.OverrideParagraphDefaultStyle(headerParagraph, 13, Unit.FromMillimeter(0d), Unit.FromMillimeter(10d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true,
                    horizontalAlignment: ParagraphAlignment.Right, underline: Underline.Single);

            var obj = section.AddParagraph("OGGETTO: Relazione dei risultati prodotti dal software Activities Inspector in merito alle attivita' condotte sul PC.");
            ReportFormatting.OverrideParagraphDefaultStyle(obj, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(13d),
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
            ReportFormatting.OverrideParagraphDefaultStyle(inquirerSurnameLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));

            var inquirerNameLabel = cell.AddParagraph();
            inquirerNameLabel.AddText("Nome investigatore: ");
            var inquirerNameValue = inquirerNameLabel.AddFormattedText(inquirerName ?? String.Empty);
            inquirerNameValue.Font.Bold = true;
            ReportFormatting.OverrideParagraphDefaultStyle(inquirerNameLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));

            var inquirerQualificationLabel = cell.AddParagraph();
            inquirerQualificationLabel.AddText("Qualifica investigatore: ");
            var inquirerQualificationValue = inquirerQualificationLabel.AddFormattedText(inquirerQualification ?? String.Empty);
            inquirerQualificationValue.Font.Bold = true;
            ReportFormatting.OverrideParagraphDefaultStyle(inquirerQualificationLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));

            var descriptionParagraph = section.AddParagraph("Descrizione del caso");
            ReportFormatting.OverrideParagraphDefaultStyle(descriptionParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(10d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);
            var descriptionValue = section.AddParagraph(description);
            ReportFormatting.OverrideParagraphDefaultStyle(descriptionValue, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(10d));

            AddPremise(section);
        }

        private void AddPremise(Section section)
        {
            var promiseParagraph = section.AddParagraph("Premessa");
            ReportFormatting.OverrideParagraphDefaultStyle(promiseParagraph, 11, Unit.FromMillimeter(0d), Unit.FromMillimeter(10d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true);

            var content = "La presente relazione e' stata prodotta attraverso il software " +
                "Activities Inspector. \n"
                + "Activities Inspector è il software che consente di estrapolare " +
                "informazioni dettagliate riguardanti l’utilizzo del PC. \n" +
                "Le informazioni elaborate coinvolgono il registro di sistema, " +
                "eventi di Windows e " +
                "file memorizzati nel file system.";
            var contentParagraph = section.AddParagraph(content);
            ReportFormatting.OverrideParagraphDefaultStyle(contentParagraph, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
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
            ReportFormatting.OverrideParagraphDefaultStyle(machineNameLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));

            var privateAddressesLabel = cell.AddParagraph();
            privateAddressesLabel.AddText("Indirizzo/i IP privati: ");
            var privateAddressesValue = privateAddressesLabel.AddFormattedText(string.Join(" ; ", privateIpAddresses) ?? String.Empty);
            privateAddressesValue.Font.Bold = true;
            ReportFormatting.OverrideParagraphDefaultStyle(privateAddressesLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));

            var publicAddressLabel = cell.AddParagraph();
            publicAddressLabel.AddText("Indirizzo IP pubblico: ");
            var publicAddressValue = publicAddressLabel.AddFormattedText(publicIpAddress ?? String.Empty);
            publicAddressValue.Font.Bold = true;
            ReportFormatting.OverrideParagraphDefaultStyle(publicAddressLabel, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d));
        }

        private static string GetProvisioningType(ProvisioningType provisioningType)
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
    }
}
