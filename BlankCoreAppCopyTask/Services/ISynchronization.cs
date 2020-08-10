using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace BlankCoreAppCopyTask.Services
{
    public interface ISynchronization
    {
        Task Copy(ImmutableArray<IFileToCopy> files, Action<double> updater);
        Task<ImmutableArray<IFileToCopy>> CreateListOfFilesToCopy(string src, string dst);
        long GetSumOfAllFileSize(ImmutableArray<IFileToCopy> files);
    }
}