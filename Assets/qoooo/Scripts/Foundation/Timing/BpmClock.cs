using System;
using System.Collections.Generic;

namespace qoooo.Foundation.Timing
{
    public sealed class BpmClock
    {
        public const double MinBpm = 1d;
        public const double MaxBpm = 1000d;
        public const double DefaultBpm = 120d;
        private const double TapTempoTimeoutSeconds = 2d;
        private const int TapTempoSetSamples = 4;
        private const int TapTempoMaxSamples = 8;

        private readonly List<double> _tapTimes = new();
        private double _pendingBpm = -1d;

        public double Bpm { get; private set; }
        public double Time { get; private set; }
        public double DeltaTime { get; private set; }
        public double Beat { get; private set; }
        public long BeatFloor => FloorToLong(Beat);

        public BpmClock(double initialBpm = DefaultBpm)
        {
            Bpm = ClampBpm(initialBpm);
        }

        public int BeatModulo(int count)
        {
            var normalizedCount = Math.Max(1, count);
            return (int)((BeatFloor % normalizedCount + normalizedCount) % normalizedCount);
        }

        public bool IsBeatModulo(int count, int target)
        {
            var normalizedCount = Math.Max(1, count);
            var normalizedTarget = ((target % normalizedCount) + normalizedCount) % normalizedCount;
            return BeatModulo(normalizedCount) == normalizedTarget;
        }

        public void SetBpm(double nextBpm) => _pendingBpm = ClampBpm(nextBpm);

        public void TapTempo()
        {
            _tapTimes.Add(Time);
            if (_tapTimes.Count > TapTempoMaxSamples) _tapTimes.RemoveAt(0);

            for (var index = _tapTimes.Count - 1; index >= 0; index--)
            {
                if (Time - _tapTimes[index] <= TapTempoTimeoutSeconds) continue;
                _tapTimes.RemoveRange(0, index + 1);
                break;
            }

            if (_tapTimes.Count < TapTempoSetSamples) return;
            var intervalCount = _tapTimes.Count - 1;
            var averageInterval = (_tapTimes[^1] - _tapTimes[0]) / intervalCount;
            if (averageInterval > 0d) SetBpm(60d / averageInterval);
        }

        public void SyncBeat() => Beat = BeatFloor;

        public void SyncBar(int barCount = 4, int beatsPerBar = 4)
        {
            var cycleBeats = (long)Math.Max(1, barCount) * Math.Max(1, beatsPerBar);
            Beat = FloorToLong(Beat / cycleBeats) * cycleBeats;
        }

        public void Update(double deltaTime)
        {
            DeltaTime = double.IsFinite(deltaTime) && deltaTime > 0d ? deltaTime : 0d;
            Time += DeltaTime;

            var previousBeat = BeatFloor;
            Beat += DeltaTime * Bpm / 60d;
            if (_pendingBpm > 0d && BeatFloor != previousBeat)
            {
                Bpm = _pendingBpm;
                _pendingBpm = -1d;
            }
        }

        private static double ClampBpm(double bpm)
        {
            if (double.IsNaN(bpm)) return DefaultBpm;
            if (double.IsNegativeInfinity(bpm)) return MinBpm;
            if (double.IsPositiveInfinity(bpm)) return MaxBpm;
            return Math.Clamp(bpm, MinBpm, MaxBpm);
        }

        private static long FloorToLong(double value) => value <= long.MinValue ? long.MinValue : value >= long.MaxValue ? long.MaxValue : (long)Math.Floor(value);
    }
}
