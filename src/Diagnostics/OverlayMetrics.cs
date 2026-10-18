using System;
using System.Collections.Generic;
namespace FrontlineMap.Diagnostics
{
    public enum WorkStage
    {
        Allocation, Stamp, Raster
    }
    public sealed class OverlayMetrics
    {
        private readonly Dictionary<WorkStage, double> cpu = new Dictionary<WorkStage, double>();
        public double MaximumChunkMilliseconds
        {
            get; private set;
        }
        public int Chunks
        {
            get; private set;
        }
        public double Milliseconds(WorkStage stage) => cpu.TryGetValue(stage, out var value) ? value : 0;
        public void Record(WorkStage stage, int cells, double seconds)
        {
            double ms = Math.Max(0, seconds * 1000);
            cpu[stage] = Milliseconds(stage) + ms;
            MaximumChunkMilliseconds = Math.Max(MaximumChunkMilliseconds, ms);
            Chunks++;
        }
        public void Reset()
        {
            cpu.Clear();
            MaximumChunkMilliseconds = 0;
            Chunks = 0;
        }
    }
}
