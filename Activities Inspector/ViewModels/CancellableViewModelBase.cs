using GalaSoft.MvvmLight.Command;
using ProgettoInformaticaForense_Argentieri.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    /// <summary>
    /// Base per i ViewModel con operazioni lunghe cancellabili.
    /// Gestisce IsBusy, il CancellationTokenSource dell'operazione in corso,
    /// il comando Annulla e l'osservazione delle eccezioni dei Task
    /// fire-and-forget (al posto degli async void).
    /// </summary>
    public abstract class CancellableViewModelBase : ValidationViewModelBase
    {
        private bool _isBusy;
        private CancellationTokenSource _cts;

        public bool IsBusy
        {
            get => _isBusy;
            protected set
            {
                if (Set(nameof(IsBusy), ref _isBusy, value))
                {
                    CancelCommand.RaiseCanExecuteChanged();
                    OnIsBusyChanged();
                }
            }
        }

        protected virtual void OnIsBusyChanged()
        {
        }

        protected IDialogService Dialogs { get; }

        protected CancellableViewModelBase(IDialogService dialogService)
        {
            Dialogs = dialogService;
        }

        private RelayCommand _cancelCommand;
        public RelayCommand CancelCommand => _cancelCommand
            ?? (_cancelCommand = new RelayCommand(
                () => _cts?.Cancel(),
                () => IsBusy));

        protected CancellationToken BeginOperation()
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            IsBusy = true;
            return _cts.Token;
        }

        protected void EndOperation()
        {
            IsBusy = false;
        }

        protected void Forget(Task task)
        {
            var scheduler = SynchronizationContext.Current != null
                ? TaskScheduler.FromCurrentSynchronizationContext()
                : TaskScheduler.Default;

            task.ContinueWith(
                t => Dialogs.ShowError(t.Exception?.ToString() ?? "Errore sconosciuto."),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                scheduler);
        }
    }
}
