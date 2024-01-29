using Autofac;
using GalaSoft.MvvmLight.Messaging;
using ProgettoInformaticaForense_Argentieri.Services;

namespace ProgettoInformaticaForense_Argentieri.ViewModels.Infrastructure
{
    public class ViewModelLocator
    {
        private static readonly IContainer _container;

        static ViewModelLocator()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<MainWindowViewModel>().SingleInstance();
            builder.RegisterType<TimeIntervalsViewModel>().SingleInstance();
            builder.RegisterType<InstalledProgramsViewModel>().SingleInstance();
            builder.RegisterType<RecentFolderViewModel>().SingleInstance();
            builder.RegisterType<PrefetchViewModel>().SingleInstance();
            builder.RegisterType<ShellBagsViewModel>().SingleInstance();
            builder.RegisterType<SessionsViewModel>().SingleInstance();
            builder.RegisterType<SystemTimeChangedViewModel>().SingleInstance();
            builder.RegisterType<UsbViewModel>().SingleInstance();
            builder.RegisterType<ReportViewModel>().SingleInstance();

            builder.RegisterType<ConfigParser>().As<IConfigParser>().SingleInstance();
            builder.RegisterType<InstallEntriesBuilder>().As<IInstallEntriesBuilder>().SingleInstance();
            builder.RegisterType<PrefetchFileInfoBuilderService>().As<IPrefetchFileInfoBuilderService>().SingleInstance();
            builder.RegisterType<PrefetchFileParserService>().As<IPrefetchFileParserService>().SingleInstance();
            builder.RegisterType<RecentFilesService>().As<IRecentFilesService>().SingleInstance();
            builder.RegisterType<ShellBagsParserService>().As<IShellBagsParserService>().SingleInstance();
            builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
            builder.RegisterType<UsageLogTimeService>().As<IUsageLogTimeService>().SingleInstance();
            builder.RegisterType<LoggedInfoService>().As<ILoggedInfoService>().SingleInstance();
            builder.RegisterType<DialogService>().As<IDialogService>().SingleInstance();
            builder.RegisterType<EntryFormatter>().As<IEntryFormatter>().SingleInstance();
            builder.RegisterType<EntriesExporter>().As<IEntriesExporter>().SingleInstance();
            builder.RegisterType<SystemTimeChangedService>().As<ISystemTimeChangedService>().SingleInstance();
            builder.RegisterType<UsbTrackingService>().As<IUsbTrackingService>().SingleInstance();
            builder.RegisterType<NetService>().As<INetService>().SingleInstance();
            builder.RegisterType<WindowFactory>().As<IWindowFactory>().SingleInstance();
            builder.RegisterType<ConfigParser>().As<IConfigParser>().SingleInstance();
            builder.RegisterType<ReportService>().As<IReportService>().SingleInstance();
            builder.RegisterInstance(Messenger.Default).As<IMessenger>().SingleInstance();

            _container = builder.Build();

            _container.Resolve<ReportViewModel>();
        }

        public MainWindowViewModel MainViewModel => _container.Resolve<MainWindowViewModel>();
        public TimeIntervalsViewModel TimeIntervalsViewModel => _container.Resolve<TimeIntervalsViewModel>();
        public InstalledProgramsViewModel InstalledProgramsViewModel => _container.Resolve<InstalledProgramsViewModel>();
        public RecentFolderViewModel RecentFolderViewModel => _container.Resolve<RecentFolderViewModel>();
        public PrefetchViewModel PrefetchViewModel => _container.Resolve<PrefetchViewModel>();
        public ShellBagsViewModel ShellBagsViewModel => _container.Resolve<ShellBagsViewModel>();
        public SessionsViewModel SessionsViewModel => _container.Resolve<SessionsViewModel>();
        public SystemTimeChangedViewModel SystemTimeChangedViewModel => _container.Resolve<SystemTimeChangedViewModel>();
        public UsbViewModel UsbViewModel => _container.Resolve<UsbViewModel>();
        public ReportViewModel ReportViewModel => _container.Resolve<ReportViewModel>();

        public static void CleanUp()
            => _container.Dispose();
    }
}
