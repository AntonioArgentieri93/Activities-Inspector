using CSharpFunctionalExtensions;
using GalaSoft.MvvmLight.Command;
using Activities_Inspector.Models;
using Activities_Inspector.Services;
using RawCopy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.ViewModels
{
    /// <summary>
    /// Base generica per le pagine di visualizzazione evidenze.
    /// Centralizza collezione, export, flusso di caricamento (con gate
    /// admin opzionale) e ordinamento per colonna. Le classi derivate
    /// forniscono solo tipo entry, servizio e messaggio di notifica.
    /// </summary>
    public abstract class FeatureViewModelBase<TEntry> : CancellableViewModelBase
        where TEntry : Entry
    {
        private ObservableCollection<TEntry> _entries;

        public ObservableCollection<TEntry> Entries
        {
            get => _entries;
            set
            {
                if (Set(nameof(Entries), ref _entries, value))
                {
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        protected IEntriesExporter Exporter { get; }
        protected IAuditTrail Audit { get; }

        protected FeatureViewModelBase(IDialogService dialogService, IEntriesExporter entriesExporter, IAuditTrail auditTrail)
            : base(dialogService)
        {
            Exporter = entriesExporter;
            Audit = auditTrail;
        }

        protected abstract EntryType EntryType { get; }
        protected virtual bool RequiresAdmin => false;
        protected virtual string ExportFooterNote => null;

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExport, CanExecuteExport));

        private bool CanExecuteExport()
            => !IsBusy && Entries != null;

        private void ExecuteExport()
            => Forget(ExportAsync());

        private async Task ExportAsync()
        {
            var token = BeginOperation();
            ExportCommand.RaiseCanExecuteChanged();

            try
            {
                var result = await Exporter.SaveEntriesDataAsync(Entries, EntryType, token, ExportFooterNote);

                if (result.IsSuccess)
                {
                    Audit.Record(AuditCategory.Export, $"{EntryType}: {Entries?.Count ?? 0} righe -> {result.Value}");

                    var message = Activities_Inspector.Resources.ExportCommand_ExportComplete_Message +
                        $"\nPercorso: {result.Value}";

                    if (!ExportLocations.IsRemovable(result.Value))
                    {
                        message += "\nAttenzione: disco locale. Per la catena di custodia esportare su un supporto rimovibile.";
                    }

                    Dialogs.ShowInfo(message);
                }
                else
                {
                    Audit.Record(AuditCategory.Export, $"{EntryType} fallito");
                    Dialogs.ShowError(result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                Audit.Record(AuditCategory.Export, $"{EntryType} annullato");
            }
            catch (Exception ex)
            {
                Dialogs.ShowError(ex.ToString());
            }
            finally
            {
                EndOperation();
                ExportCommand.RaiseCanExecuteChanged();
            }
        }

        protected async Task LoadAsync()
        {
            if (Entries != null) Entries.Clear();
            var token = BeginOperation();
            ExportCommand.RaiseCanExecuteChanged();

            try
            {
                if (!CheckAccess()) return;

                var result = await LoadEntriesAsync(token);

                if (result.IsSuccess)
                {
                    Audit.Record(AuditCategory.Ricerca, $"{EntryType}: {result.Value.Count} risultati");
                    SetEntries(new ObservableCollection<TEntry>(result.Value));
                    PublishEntries(result.Value);
                    AfterLoad();
                }
                else
                {
                    Audit.Record(AuditCategory.Ricerca, $"{EntryType} fallita");
                    Dialogs.ShowError(result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                Audit.Record(AuditCategory.Ricerca, $"{EntryType} annullata");
            }
            catch (Exception ex)
            {
                Dialogs.ShowError(ex.ToString());
            }
            finally
            {
                EndOperation();
                ExportCommand.RaiseCanExecuteChanged();
            }
        }

        protected virtual bool CheckAccess()
        {
            if (RequiresAdmin && !Helper.IsAdministrator())
            {
                Dialogs.ShowInfo("Per eseguire questa funzionalità occorre essere amministratori. " +
                    "Riavviare l'applicazione in Modalità Amministratore.");
                return false;
            }

            return true;
        }

        protected abstract Task<Result<List<TEntry>>> LoadEntriesAsync(CancellationToken token);
        protected abstract void SetEntries(ObservableCollection<TEntry> entries);
        protected abstract void PublishEntries(List<TEntry> entries);

        protected virtual void AfterLoad()
        {
        }

        protected void ApplySort(object propertyType, bool ascending)
        {
            if (Entries == null || Entries.Count == 0) return;

            var property = typeof(TEntry).GetProperty(propertyType != null ? propertyType.ToString() : string.Empty);
            if (property == null) return;

            var ordered = ascending
                ? Entries.OrderBy(e => property.GetValue(e, null)).ToList()
                : Entries.OrderByDescending(e => property.GetValue(e, null)).ToList();

            SetEntries(new ObservableCollection<TEntry>(ordered));
            PublishEntries(ordered);
        }
    }
}
