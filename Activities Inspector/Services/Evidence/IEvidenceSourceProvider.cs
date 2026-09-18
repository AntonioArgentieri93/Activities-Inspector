namespace Activities_Inspector.Services.Evidence
{
    public interface IEvidenceSourceProvider
    {
        IEvidenceSource Current { get; }

        void UseLive();

        void UseImage(string rootPath);
    }
}
