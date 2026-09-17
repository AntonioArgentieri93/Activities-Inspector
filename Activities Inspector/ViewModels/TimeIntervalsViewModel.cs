using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Activities_Inspector.Messages;
using Activities_Inspector.Models;
using Activities_Inspector.Pages;
using Activities_Inspector.Services;
using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.ViewModels
{
    public class TimeIntervalsViewModel : FeatureViewModelBase<UsageInfo>
    {
        #region Proprietà

        public ObservableCollection<UsageInfo> Infos
        {
            get => Entries;
            set
            {
                Entries = value;
                RaisePropertyChanged(nameof(Infos));
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadIntervalsCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadIntervalsCommand;
        public RelayCommand LoadIntervalsCommand => _loadIntervalsCommand
            ?? (_loadIntervalsCommand = new RelayCommand(ExecuteLoadIntervalsCommand,
                CanExecuteLoadIntervalsCommand));

        #endregion

        private readonly IUsageLogTimeService _usageLogTimeService;
        private readonly IMessenger _messenger;

        public TimeIntervalsViewModel(IUsageLogTimeService usageLogTimeService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService, entriesExporter)
        {
            _usageLogTimeService = usageLogTimeService;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadIntervalsCommand()
            => !IsBusy;

        private void ExecuteLoadIntervalsCommand()
            => Forget(LoadAsync());

        protected override EntryType EntryType => EntryType.TimeIntervals;

        protected override async Task<Result<List<UsageInfo>>> LoadEntriesAsync(CancellationToken token)
        {
            var eventsResult = await _usageLogTimeService.GetSystemEventsAsync(token);

            if (!eventsResult.IsSuccess)
                return Result.Failure<List<UsageInfo>>(eventsResult.Error);

            return Result.Success(_usageLogTimeService.BuildUsageInfo(eventsResult.Value).ToList());
        }

        protected override void SetEntries(ObservableCollection<UsageInfo> entries)
        {
            Infos = entries;
        }

        protected override void PublishEntries(List<UsageInfo> entries)
        {
            _messenger.Send(new OnUsageInfosChangedMessage(entries));
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
                    case UsageInfoPropertyType.StartedAfterCrash:
                        Infos = new ObservableCollection<UsageInfo>(Infos.OrderBy(d => d.Interval.StartedAfterCrash));
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
                    case UsageInfoPropertyType.StartedAfterCrash:
                        Infos = new ObservableCollection<UsageInfo>(Infos.OrderByDescending(d => d.Interval.StartedAfterCrash));
                        break;
                }
            }

            _messenger.Send(new OnUsageInfosChangedMessage(Infos.ToList()));
        }
    }
}