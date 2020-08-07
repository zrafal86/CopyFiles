namespace BlankCoreAppCopyTask.Services
{
    public class FileToCopy : IFileToCopy
    {
        public FileToCopy(string hash, string file, long length, string destination)
        {
            Hash = hash;
            Path = file;
            Size = length;
            Destination = destination;
        }

        public string Hash { get; }
        public string Path { get; }
        public long Size { get; }
        public string Destination { get; }
    }
}