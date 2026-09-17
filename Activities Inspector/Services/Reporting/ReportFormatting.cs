using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using System.Collections.Generic;
using System.Text;
using Table = MigraDocCore.DocumentObjectModel.Tables.Table;

namespace Activities_Inspector.Services.Reporting
{
    internal static class ReportFormatting
    {
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

        internal static void AddHeaderToTable(Table table, List<string> labels)
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
