﻿using System;
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
    public class SynchronizationPlaylistSlow : ISynchronizationPlaylist
    {
        private readonly IHashCalculator _hashCalculator;

        public SynchronizationPlaylistSlow(IHashCalculator hashCalculator)
        {
            _hashCalculator = hashCalculator;
        }

        public async Task Copy(ImmutableArray<IFileToCopy> files, Action<double> updater)
        {
            var sum = GetSumOfAllFileSize(files);

            var copyProgressInfo = 0L;
            var action = new Action<long>(bytesTransferred =>
            {
                Interlocked.Add(ref copyProgressInfo, bytesTransferred);
                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    updater?.Invoke((double)copyProgressInfo / sum);
                });
            });
            var copyOperation = Task.Run(async () =>
            {
                foreach (var file in files)
                { 
                    Debug.WriteLine($"Start ManagedThreadId: {Thread.CurrentThread.ManagedThreadId}");
                    await Copy(file, action);
                    Debug.WriteLine($"End ManagedThreadId: {Thread.CurrentThread.ManagedThreadId}");
                }
            });

            await copyOperation;
        }

        private async Task Copy(IFileToCopy file, Action<long> progressUpdate)
        {
            await using var sourceStream = File.Open(file.Path, FileMode.Open);
            if (!File.Exists(file.Destination))
            {
                await using var destinationStream = File.Create(file.Destination);
                await sourceStream.CopyToAsync(destinationStream, 16384, progressUpdate, CancellationToken.None);
            }
        }

        public async Task<ImmutableArray<IFileToCopy>> CreateListOfFilesToCopy(string src, string dst)
        {
            var filesToCopy = new ConcurrentBag<IFileToCopy>();

            Directory.CreateDirectory(dst);

            await Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(src))
                {
                    var fi = new FileInfo(file);
                    var hash = _hashCalculator.CalculateHash(file, MD5.Create());
                    Debug.WriteLine($"{Thread.CurrentThread.Name}\nfile: {file}: hash: {hash}");
                    var destination = Path.Combine(dst, $"{hash}{fi.Extension}");
                    var isAlreadyAdded = filesToCopy.Any(fileToCopy => fileToCopy.Hash.Equals(hash, StringComparison.OrdinalIgnoreCase));
                    if (!File.Exists(destination) && !isAlreadyAdded)
                    {
                        var fileToCopy = new FileToCopy(hash, file, fi.Length, destination);
                        filesToCopy.Add(fileToCopy);
                    }
                }
            });

            return filesToCopy.ToImmutableArray();
        }

        public long GetSumOfAllFileSize(ImmutableArray<IFileToCopy> files)
        {
            return files.Sum(file => file.Size);
        }
    }
}
