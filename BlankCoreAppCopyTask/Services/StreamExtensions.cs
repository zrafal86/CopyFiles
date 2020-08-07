using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlankCoreAppCopyTask.Services
{
    public static class StreamExtensions
    {

        internal static async Task CopyToAsync(this Stream fromStream, Stream destination, int bufferSize, Action<long> progressUpdate, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];
            int count;
            while ((count = await fromStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) != 0)
            {
                await destination.WriteAsync(buffer, 0, count, cancellationToken);
                progressUpdate?.Invoke(count);
            }
        }
    }
}
