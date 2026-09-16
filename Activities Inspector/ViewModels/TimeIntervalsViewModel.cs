using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ProgettoInformaticaForense_Argentieri.Messages;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Pages;
using ProgettoInformaticaForense_Argentieri.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class TimeIntervalsViewModel : CancellableViewModelBase
    {
        #region Proprietà

        private ObservableCollection<UsageInfo> _infos;

        public ObservableCollection<UsageInfo> Infos
        {
            get => _infos;
            set
            {
                var changed = Set(nameof(Infos), ref _infos, value);

                if (changed)
                {
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadIntervalsCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadIntervalsCommand;
        public RelayCommand LoadIntervalsCommand => _loadIntervalsCommand
            ?? (_loadIntervalsCommand = new RelayCommand(ExecuteLoadIntervalsCommand,
                CanExecuteLoadIntervalsCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommand,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly IUsageLogTimeService _usageLogTimeService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public TimeIntervalsViewModel(IUsageLogTimeService usageLogTimeService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService)
        {
            _usageLogTimeService = usageLogTimeService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadIntervalsCommand()
            => !IsBusy;

        private void ExecuteLoadIntervalsCommand()
            => Forget(LoadIntervalsAsync());

        private async Task LoadIntervalsAsync()
        {
            if (Infos != null) Infos.Clear();
            var token = BeginOperation();

            try
            {
                var getSystemEventsResult = await _usageLogTimeService.GetSystemEventsAsync(token);

                if (getSystemEventsResult.IsSuccess)
                {
                    var events = getSystemEventsResult.Value;
                    Infos = new ObservableCollection<UsageInfo>(_usageLogTimeService.BuildUsageInfo(events).ToList());

                    _messenger.Send(new OnUsageInfosChangedMessage(Infos.ToList()));
                }
                else
                {
                    Dialogs.ShowError(getSystemEventsResult.Error);
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

        private bool CanExecuteExportCommandAsync()
            => !IsBusy && Infos != null;

        private void ExecuteExportCommand()
            => Forget(ExportAsync());

        private async Task ExportAsync()
        {
            var token = BeginOperation();

            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(Infos, EntryType.TimeIntervals, token);

                if (exportResult.IsSuccess)
                {
                    Dialogs.ShowInfo(Activities_Inspector.Resources.ExportCommand_ExportComplete_Message);
                }
                else
                {
                    Dialogs.ShowError(exportResult.Error);
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

        private void HandleOnSortColumnMessage(OnSortColumnMessage message)
        {
            if (Infos == null || Infos.Count == 0) return;

            var propertyType = (UsageInfoPropertyType)message.NewPropertyType;
            var isAscending = message.NewIsAscending;

            if (isAscending)
            {
                switch (propertyType)
                {
                    case UsageInfoPropertyType.IntervalStart:
                        Infos = new ObservableCollection<UsageInfo>(Infos.OrderBy(d => d.Interval.Start));
                        break;
                    case UsageInfoPropertyType.IntervalEnd:
                        Infos = new ObservableCollection<UsageInfo>(Infos.OrderBy(d => d.Interval.End));
                        break;
                    case UsageInfoPropertyType.Duration:
                        Infos = new ObservableCollection<UsageInfo>(Infos.OrderBy(d => d.Duration));
                        break;
                    case UsageInfoPropertyType.MachineName:
                        Infos = new ObservableCollection<UsageInfo>(Infos.OrderBy(d => d.MachineName));
                        break;
                }
            }
            else
            {
                switch (propertyType)
                {
                    case UsageInfoPropertyType.IntervalStart:
                        Infos = new ObservableCollection<UsageInfo>(Infos.OrderByDescending(d => d.Interval.Start));
                        break;
                    case UsageInfoPropertyType.IntervalEnd:
                        Infos = new ObservableCollection<UsageInfo>(Infos.OrderByDescending(d => d.Interval.End));
                        break;
                    case UsageInfoPropertyType.Duration:
                        Infos = new ObservableCollection<UsageInfo>(Infos.OrderByDescending(d => d.Duration));
                        break;
                    case UsageInfoPropertyType.MachineName:
                        Infos = new ObservableCollection<UsageInfo>(Infos.OrderByDescending(d => d.MachineName));
                        break;
                }
            }

            _messenger.Send(new OnUsageInfosChangedMessage(Infos.ToList()));
        }
    }
}