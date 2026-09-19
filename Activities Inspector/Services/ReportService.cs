using Activities_Inspector.Models;
using Activities_Inspector.Services.Reporting;
using CSharpFunctionalExtensions;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.Rendering;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Document = MigraDocCore.DocumentObjectModel.Document;

namespace Activities_Inspector.Services
{
    public class ReportService : IReportService
    {
        private readonly ReportCoverBuilder _coverBuilder;

        public ReportService(INetService netService)
        {
            _coverBuilder = new ReportCoverBuilder(netService);
        }

        public async Task<Result> CreatePdfFileAsync(ReportContent content, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var filePath = Path.Combine(content.DestinationPath, $"Report_{DateTime.Now:dd-M-yyyy}.pdf");

                // Rendering del PDF (CPU-bound, decine di pagine) su thread
                // pool: senza questo la finestra resta congelata e lo
                // spinner non viene mai renderizzato.
                var doc = await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var document = new Document();
                    var coverSection = document.AddSection();

                    _coverBuilder.BuildCover(content, document, coverSection);

                    var tablesSection = document.AddSection();
                    ReportCoverBuilder.ConfigureTablesSection(tablesSection);
                    ReportTablesBuilder.AddContents(content.UsageInfos, content.InstallEntries, content.RecentFolderEntries, content.PrefetchInfoEntries, content.ShellBagEntries,
                        content.SessionEntries, content.SystemTimeChangedEntries, content.UsbEntries, tablesSection,
                        ReportFormatting.LandscapeContentWidthMillimeters, content.ShellBagsPartial, content.IntegrityManifest, content.AuditTrail,
                        content.UsageSource, content.InstallSource, content.RecentSource, content.PrefetchSource, content.ShellBagsSource, content.SessionsSource, content.TimeChangedSource, content.UsbSource);

                    ReportFormatting.AddFooterWithPageNumbers(coverSection);
                    ReportFormatting.AddFooterWithPageNumbers(tablesSection);

                    return FinalizeDocument(document);
                }, cancellationToken);

                await File.WriteAllBytesAsync(filePath, doc, cancellationToken);
                Utils.IntegrityHasher.WriteSidecar(filePath, doc);

                return Result.Success();
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure(ex.ToString());
            }
        }

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
    }
}
