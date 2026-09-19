using Activities_Inspector.Models;

namespace Activities_Inspector.Models
{
    public class ReportContent
    {
        public ProvisioningType ProvisioningType { get; }
        public string Other {  get; }
        public string InquirerSurname { get; }
        public string InquirerName { get; }
        public string InquirerQualification { get; }
        public string ObjectDescription { get; }
        public UsageInfo[] UsageInfos { get; }
        public InstallEntry[] InstallEntries { get; }
        public RecentFolderEntry[] RecentFolderEntries { get; }
        public PrefetchInfoEntry[] PrefetchInfoEntries { get; }
        public ShellBagEntry[] ShellBagEntries { get; }
        public SessionEntry[] SessionEntries { get; }
        public SystemTimeChangedEntry[] SystemTimeChangedEntries { get; }
        public UsbEntry[] UsbEntries { get; }
        public string DestinationPath { get; }
        public bool ShellBagsPartial { get; }
        public IntegrityRecord[] IntegrityManifest { get; }
        public AuditEntry[] AuditTrail { get; }
        public string EvidenceSource { get; }
        public string UsageSource { get; }
        public string InstallSource { get; }
        public string RecentSource { get; }
        public string PrefetchSource { get; }
        public string ShellBagsSource { get; }
        public string SessionsSource { get; }
        public string TimeChangedSource { get; }
        public string UsbSource { get; }

        public ReportContent(ProvisioningType provisioningType, string other, string inquirerSurname,
            string inquirerName, string inquirerQualification, string objectDescription, UsageInfo[] usageInfos,
            InstallEntry[] installEntries, RecentFolderEntry[] recentFolderEntries,PrefetchInfoEntry[] prefetchInfoEntries, 
            ShellBagEntry[] shellBagEntries, SessionEntry[] sessionEntries, 
            SystemTimeChangedEntry[] systemTimeChangedEntries, UsbEntry[] usbEntries, string destinationPath,
            bool shellBagsPartial = false, IntegrityRecord[] integrityManifest = null,
            AuditEntry[] auditTrail = null, string evidenceSource = null,
            string usageSource = null, string installSource = null, string recentSource = null, string prefetchSource = null,
            string shellBagsSource = null, string sessionsSource = null, string timeChangedSource = null, string usbSource = null)
        {
            this.ProvisioningType = provisioningType;
            this.Other = other;
            this.InquirerSurname = inquirerSurname;
            this.InquirerName = inquirerName;
            this.InquirerQualification = inquirerQualification;
            this.ObjectDescription = objectDescription;
            this.UsageInfos = usageInfos;
            this.InstallEntries = installEntries;
            this.RecentFolderEntries = recentFolderEntries;
            this.PrefetchInfoEntries = prefetchInfoEntries;
            this.ShellBagEntries = shellBagEntries;
            this.SessionEntries = sessionEntries;   
            this.SystemTimeChangedEntries = systemTimeChangedEntries;
            this.UsbEntries = usbEntries;   
            this.DestinationPath = destinationPath; 
            this.ShellBagsPartial = shellBagsPartial;
            this.IntegrityManifest = integrityManifest ?? new IntegrityRecord[0];
            this.AuditTrail = auditTrail ?? new AuditEntry[0];
            this.EvidenceSource = evidenceSource ?? "Sistema live";
            this.UsageSource = usageSource;
            this.InstallSource = installSource;
            this.RecentSource = recentSource;
            this.PrefetchSource = prefetchSource;
            this.ShellBagsSource = shellBagsSource;
            this.SessionsSource = sessionsSource;
            this.TimeChangedSource = timeChangedSource;
            this.UsbSource = usbSource;
        }
    }
}
