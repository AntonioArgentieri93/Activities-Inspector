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

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class TimeIntervalsViewModel : ViewModelBase
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

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                var changed = Set(nameof(IsBusy), ref _isBusy, value);

                if (changed)
                {
                    LoadIntervalsCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Comandi

        private RelayCommand _loadIntervalsCommand;
        public RelayCommand LoadIntervalsCommand => _loadIntervalsCommand
            ?? (_loadIntervalsCommand = new RelayCommand(ExecuteLoadIntervalsCommandAsync,
                CanExecuteLoadIntervalsCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommandAsync,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly IUsageLogTimeService _usageLogTimeService;
        private readonly IDialogService _dialogService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public TimeIntervalsViewModel(IUsageLogTimeService usageLogTimeService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
        {
            _usageLogTimeService = usageLogTimeService;
            _dialogService = dialogService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadIntervalsCommand()
            => IsBusy ? false : true;

        private async void ExecuteLoadIntervalsCommandAsync()
        {
            if (Infos != null) Infos.Clear();
            IsBusy = true;

            try
            {
                var getSystemEventsResult = await _usageLogTimeService.GetSystemEventsAsync();

                if (getSystemEventsResult.IsSuccess)
                {
                    var events = getSystemEventsResult.Value;
                    Infos = new ObservableCollection<UsageInfo>(_usageLogTimeService.BuildUsageInfo(events).ToList());

                    _messenger.Send(new OnUsageInfosChangedMessage(Infos.ToList()));
                }
                else
                {
                    _dialogService.ShowError(getSystemEventsResult.Error);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message + "\n" + ex.StackTrace);
            }

            IsBusy = false;
        }

        private bool CanExecuteExportCommandAsync()
            => IsBusy == false & Infos != null;

        private async void ExecuteExportCommandAsync()
        {
            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(Infos, EntryType.TimeIntervals);

                if (exportResult.IsSuccess)
                {
                    _dialogService.ShowInfo(Activities_Inspector.Resources.ExportCommand_ExportComplete_Message);
                }
                else
                {
                    _dialogService.ShowError(exportResult.Error);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message);
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
