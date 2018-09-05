using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace BlazorSignalR.Internal
{
    internal static class ClientPipeOptions
    {
        public static PipeOptions DefaultOptions = new PipeOptions(writerScheduler: PipeScheduler.ThreadPool,
            readerScheduler: PipeScheduler.ThreadPool, useSynchronizationContext: false, pauseWriterThreshold: 0,
            resumeWriterThreshold: 0);
    }
}