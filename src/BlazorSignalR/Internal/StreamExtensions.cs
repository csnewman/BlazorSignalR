using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorSignalR.Internal
{
    internal static class StreamExtensions
    {
        public static ValueTask WriteAsync(this Stream stream, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (buffer.IsSingleSegment)
            {
#if NETCOREAPP2_2
                return stream.WriteAsync(buffer.First, cancellationToken);
#else
                var isArray = MemoryMarshal.TryGetArray(buffer.First, out var arraySegment);
                // We're using the managed memory pool which is backed by managed buffers
                Debug.Assert(isArray);
                return new ValueTask(stream.WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cancellationToken));
#endif
            }

            return WriteMultiSegmentAsync(stream, buffer, cancellationToken);
        }

        private static async ValueTask WriteMultiSegmentAsync(Stream stream, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            var position = buffer.Start;
            while (buffer.TryGet(ref position, out var segment))
            {
#if NETCOREAPP2_2
                await stream.WriteAsync(segment, cancellationToken);
#else
                var isArray = MemoryMarshal.TryGetArray(segment, out var arraySegment);
                // We're using the managed memory pool which is backed by managed buffers
                Debug.Assert(isArray);
                await stream.WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cancellationToken);
#endif
            }
        }
    }
}
