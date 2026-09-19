using NUnit.Framework;
using qoooo.Foundation.Timing;

namespace qoooo.Tests.EditMode
{
    public sealed class BpmClockTests
    {
        [Test]
        public void PendingBpm_IsAppliedOnlyAfterCrossingBeatBoundary()
        {
            var clock = new BpmClock(120d);
            clock.Update(0.49d);
            clock.SetBpm(60d);
            clock.Update(0.01d);
            Assert.That(clock.Bpm, Is.EqualTo(60d));
        }

        [Test]
        public void BeatModulo_UsesTheCurrentIntegerBeat()
        {
            var clock = new BpmClock();
            clock.Update(2.5d);
            Assert.That(clock.BeatFloor, Is.EqualTo(5));
            Assert.That(clock.BeatModulo(4), Is.EqualTo(1));
        }

        [Test]
        public void TapTempo_QueuesAverageTempoAfterFourSamples()
        {
            var clock = new BpmClock(90d);
            clock.TapTempo();
            clock.Update(0.5d);
            clock.TapTempo();
            clock.Update(0.5d);
            clock.TapTempo();
            clock.Update(0.5d);
            clock.TapTempo();
            clock.Update(0.5d);
            Assert.That(clock.Bpm, Is.EqualTo(120d).Within(0.001d));
        }
    }
}
