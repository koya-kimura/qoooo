using UnityEngine;

namespace qoooo.Foundation.Timing
{
    [DefaultExecutionOrder(-120)]
    public sealed class BpmManager : MonoBehaviour, IBpmSource
    {
        [SerializeField, Range(1f, 1000f)] private float _initialBpm = (float)BpmClock.DefaultBpm;

        private BpmClock _clock;

        public double Bpm => Clock.Bpm;
        public double Time => Clock.Time;
        public double DeltaTime => Clock.DeltaTime;
        public double Beat => Clock.Beat;
        public long BeatFloor => Clock.BeatFloor;

        private BpmClock Clock => _clock ??= new BpmClock(_initialBpm);

        private void Awake()
        {
            _clock = new BpmClock(_initialBpm);
            Debug.Log($"[BPM] Initialized at {Bpm:F2} BPM.", this);
        }

        private void Update() => Clock.Update(UnityEngine.Time.unscaledDeltaTime);

        public int BeatModulo(int count) => Clock.BeatModulo(count);
        public bool IsBeatModulo(int count, int target) => Clock.IsBeatModulo(count, target);

        public void SetBpm(double bpm)
        {
            Clock.SetBpm(bpm);
            Debug.Log($"[BPM] BPM change queued: {bpm:F2}.", this);
        }

        public void TapTempo()
        {
            Clock.TapTempo();
            Debug.Log($"[BPM] Tap tempo received at {Time:F3}s.", this);
        }

        public void SyncBeat()
        {
            Clock.SyncBeat();
            Debug.Log($"[BPM] Beat synchronized: {Beat:F3}.", this);
        }

        public void SyncBar(int barCount = 4, int beatsPerBar = 4)
        {
            Clock.SyncBar(barCount, beatsPerBar);
            Debug.Log($"[BPM] Bar synchronized: {Beat:F3}.", this);
        }
    }
}
