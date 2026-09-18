using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using System.Collections.Generic;
using System.Text;
using Table = MigraDocCore.DocumentObjectModel.Tables.Table;

namespace Activities_Inspector.Services.Reporting
{
    internal static class ReportSectionCatalog
    {
        internal const string UsageKey = "usage";
        internal const string UsageTitle = "Orari di accensione e spegnimento";

        internal const string InstalledKey = "installed";
        internal const string InstalledTitle = "Programmi installati";

        internal const string RecentsKey = "recents";
        internal const string RecentsTitle = "File recenti";

        internal const string PrefetchKey = "prefetch";
        internal const string PrefetchTitle = "Prefetch";

        internal const string ShellbagsKey = "shellbags";
        internal const string ShellbagsTitle = "Shellbags";

        internal const string SessionsKey = "sessions";
        internal const string SessionsTitle = "LogOn/LogOff";

        internal const string TimeChangedKey = "timechanged";
        internal const string TimeChangedTitle = "Modifiche all'ora di Sistema";

        internal const string UsbKey = "usb";
        internal const string UsbTitle = "Periferiche USB";

        internal const string IntegrityKey = "integrity";
        internal const string IntegrityTitle = "Integrità e catena di custodia";

        internal const string AuditKey = "audit";
        internal const string AuditTitle = "Diario operativo";

        internal static readonly (string Key, string Title)[] All =
        {
            (UsageKey, UsageTitle),
            (InstalledKey, InstalledTitle),
            (RecentsKey, RecentsTitle),
            (PrefetchKey, PrefetchTitle),
            (ShellbagsKey, ShellbagsTitle),
            (SessionsKey, SessionsTitle),
            (TimeChangedKey, TimeChangedTitle),
            (UsbKey, UsbTitle),
            (IntegrityKey, IntegrityTitle),
            (AuditKey, AuditTitle)
        };
    }

    internal static class ReportFormatting
    {
        internal const double PortraitContentWidthMillimeters = 192;
        internal const double LandscapeContentWidthMillimeters = 277; // A4 landscape meno margini da 1 cm
        internal const string NoResultsNoteText = "Nessun elemento rilevato per questa funzionalita'.";
        internal const string PartialResultsWarningText = "Attenzione: risultati parziali, la raccolta e' stata interrotta da un errore.";

        internal static void AddPartialResultsWarning(Section section)
        {
            var warning = section.AddParagraph(PartialResultsWarningText);
            OverrideParagraphDefaultStyle(warning, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                Unit.FromMillimeter(0d), Unit.FromMillimeter(5d), bold: true);
        }
        internal static void OverrideParagraphDefaultStyle(Paragraph paragraph, Unit size, Unit marginLeft,
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

        internal static void AddNewPage(Section section)
        {
            var pageBreak = section.AddParagraph();
            pageBreak.Format.PageBreakBefore = true;
        }

        internal static void AddHeaderToTable(Table table, List<string> labels, double totalWidthMm = PortraitContentWidthMillimeters)
        {
            var columnsNumber = labels.Count;
            var columnWidth = (float)totalWidthMm / columnsNumber;

            for (var i=0; i<columnsNumber; i++)
            {
                table.AddColumn(Unit.FromMillimeter(columnWidth)); 
            }

            var row = table.AddRow();
            row.HeadingFormat = true;

            for (var i = 0; i < columnsNumber; i++)
            {
                var cell = row.Cells[i];

                var paragraph = cell.AddParagraph(labels[i]);

                OverrideParagraphDefaultStyle(paragraph, 9, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                    Unit.FromMillimeter(0d), Unit.FromMillimeter(1.5d), bold: true,
                    horizontalAlignment: ParagraphAlignment.Center);
            }
        }

        internal static void AddFooterWithPageNumbers(Section section)
        {
            var paragraph = section.Footers.Primary.AddParagraph();
            paragraph.AddText("Activities Inspector - Pagina ");
            paragraph.AddPageField();
            paragraph.AddText(" di ");
            paragraph.AddNumPagesField();

            OverrideParagraphDefaultStyle(paragraph, 8, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                horizontalAlignment: ParagraphAlignment.Center);
        }

        internal static void AddNoResultsNote(Section section)
        {
            var note = section.AddParagraph(NoResultsNoteText);
            OverrideParagraphDefaultStyle(note, 10, Unit.FromMillimeter(0d), Unit.FromMillimeter(0d),
                Unit.FromMillimeter(0d), Unit.FromMillimeter(5d));
            note.Format.Font.Italic = true;
        }

        internal static void AddRowValuesToTable(Table table, List<string> values)
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
                paragraph.Format.Font.Size = 9;
                paragraph.Format.Font.Name = "Arial";
                cell.Format.LeftIndent = 0; // Il contenuto della cella non ha margini a sx
            }
        }

        internal static string AddWordBreaks(string text)
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
    }
}
