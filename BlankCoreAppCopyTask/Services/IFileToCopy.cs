namespace BlankCoreAppCopyTask.Services
{
    public interface IFileToCopy
    {
        public string Hash { get; }
        public string Path { get; }
        public long Size { get; }
        public string Destination { get; }
    }
}