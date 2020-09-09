using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace BlankCoreAppCopyTask.Services
{
    public interface ISynchronization
    {
        const int BUFFER_SIZE = 4 * 1024;
        Task<Result<IFileToCopy>[]> Copy(ImmutableArray<IFileToCopy> files, Action<double> updater, CancellationToken sourceToken);
        Task<ImmutableArray<IFileToCopy>> CreateListOfFilesToCopy(string src, string dst);
        long GetSumOfAllFileSize(ImmutableArray<IFileToCopy> files);
    }
}