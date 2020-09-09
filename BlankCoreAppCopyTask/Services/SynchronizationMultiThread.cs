using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace BlankCoreAppCopyTask.Services
{
    public class SynchronizationMultiThread : ISynchronization
    {
        private readonly IHashCalculator _hashCalculator;

        public SynchronizationMultiThread(IHashCalculator hashCalculator)
        {
            _hashCalculator = hashCalculator;
        }

        public async Task<Result<IFileToCopy>[]> Copy(ImmutableArray<IFileToCopy> files, Action<double> updater,
            CancellationToken sourceToken)
        {
            var sum = GetSumOfAllFileSize(files);

            var copyProgressInfo = 0L;
            var action = new Action<long>(bytesTransferred =>
            {
                Interlocked.Add(ref copyProgressInfo, bytesTransferred);
                Dispatcher.CurrentDispatcher.Invoke(() => { updater?.Invoke((double) copyProgressInfo / sum); });
            });

            var tasks = files.Select(file => Task.Factory.StartNew(
                async () => await Copy(file, action, sourceToken),
                sourceToken).Unwrap()).ToList();

            return await Task.WhenAll(tasks);
        }

        public async Task<ImmutableArray<IFileToCopy>> CreateListOfFilesToCopy(string src, string dst)
        {
            var filesToCopy = new ConcurrentBag<IFileToCopy>();

            Directory.CreateDirectory(dst);

            await Task.Factory.StartNew(() =>
            {
                foreach (var file in Directory.GetFiles(src))
                    Task.Factory.StartNew(() =>
                    {
                        var fi = new FileInfo(file);
                        var hash = _hashCalculator.CalculateHash(file, MD5.Create());
                        Debug.WriteLine($"{Thread.CurrentThread.ManagedThreadId}\nfile: {file}: hash: {hash}");
                        var destination = Path.Combine(dst, $"{hash}{fi.Extension}");
                        var isAlreadyAdded = filesToCopy.Any(fileToCopy =>
                            fileToCopy.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));
                        if (!File.Exists(destination) && !isAlreadyAdded)
                        {
                            var fileToCopy = new FileToCopy(hash, file, fi.Length, destination);
                            filesToCopy.Add(fileToCopy);
                        }
                    }, TaskCreationOptions.AttachedToParent);
            });

            return filesToCopy.ToImmutableArray();
        }

        public long GetSumOfAllFileSize(ImmutableArray<IFileToCopy> files)
        {
            return files.Sum(file => file.Size);
        }

        private static async Task<Result<IFileToCopy>> Copy(IFileToCopy file, Action<long> progressUpdate,
            CancellationToken sourceToken = default)
        {
            try
            {
                if (sourceToken.IsCancellationRequested)
                {
                    sourceToken.ThrowIfCancellationRequested();
                }

                await using (var sourceStream = File.Open(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (!File.Exists(file.Destination) && sourceStream != null)
                    {
                        await using var destinationStream = File.Create(file.Destination);
                        await sourceStream.CopyToAsync(destinationStream, ISynchronization.BUFFER_SIZE, progressUpdate, sourceToken);
                    }
                }

                return new Result<IFileToCopy>(file, null);
            }
            catch (FileNotFoundException e)
            {
                Debug.WriteLine(e);
                return new Result<IFileToCopy>(file, new Error($"file not found: {e.Message}"));
            }
            catch (IOException e)
            {
                Debug.WriteLine(e);
                return new Result<IFileToCopy>(file, new Error($"io error: {e.Message}"));
            }
        }
    }
}