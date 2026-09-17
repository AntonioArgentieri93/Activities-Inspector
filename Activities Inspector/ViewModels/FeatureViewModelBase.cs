using CSharpFunctionalExtensions;
using GalaSoft.MvvmLight.Command;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Services;
using RawCopy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
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

        protected FeatureViewModelBase(IDialogService dialogService, IEntriesExporter entriesExporter)
            : base(dialogService)
        {
            Exporter = entriesExporter;
        }

        protected abstract EntryType EntryType { get; }
        protected virtual bool RequiresAdmin => false;

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

            try
            {
                var result = await Exporter.SaveEntriesDataAsync(Entries, EntryType, token);

                if (result.IsSuccess)
                {
                    Dialogs.ShowInfo(Activities_Inspector.Resources.ExportCommand_ExportComplete_Message);
                }
                else
                {
                    Dialogs.ShowError(result.Error);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Dialogs.ShowError(ex.ToString());
            }
            finally
            {
                EndOperation();
            }
        }

        protected async Task LoadAsync()
        {
            if (Entries != null) Entries.Clear();
            var token = BeginOperation();

            try
            {
                if (!CheckAccess()) return;

                var result = await LoadEntriesAsync(token);

                if (result.IsSuccess)
                {
                    SetEntries(new ObservableCollection<TEntry>(result.Value));
                    PublishEntries(result.Value);
                }
                else
                {
                    Dialogs.ShowError(result.Error);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Dialogs.ShowError(ex.ToString());
            }
            finally
            {
                EndOperation();
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
