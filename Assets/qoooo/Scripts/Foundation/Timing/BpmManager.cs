using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Presentation.Contracts;
using RosettaUI;
using UnityEngine;

namespace qoooo.Foundation.Timing
{
    [DefaultExecutionOrder(-120)]
    public sealed class BpmManager : MonoBehaviour, IBpmSource, IMidiParameterContainer, IUiTarget
    {
        [SerializeField, Range(1f, 1000f)] private float _initialBpm = (float)BpmClock.DefaultBpm;
        [SerializeField] private FloatParameter _bpm = new("rhythm.bpm", "Rhythm BPM", 1f, 1000f, 120f);
        [SerializeField] private OneshotParameter _tap = new("rhythm.tap", "Rhythm Tap");
        [SerializeField] private OneshotParameter _syncBeat = new("rhythm.sync-beat", "Rhythm Sync Beat");
        [SerializeField] private OneshotParameter _syncBar = new("rhythm.sync-bar", "Rhythm Sync Bar");

        private BpmClock _clock;
        private bool _synchronizingBpmParameter;
        private bool _hasQueuedBpm;

        public double Bpm => Clock.Bpm;
        public double Time => Clock.Time;
        public double DeltaTime => Clock.DeltaTime;
        public double Beat => Clock.Beat;
        public long BeatFloor => Clock.BeatFloor;

        private BpmClock Clock => _clock ??= new BpmClock(_initialBpm);

        private void Awake()
        {
            _bpm ??= new FloatParameter("rhythm.bpm", "Rhythm BPM", 1f, 1000f, 120f);
            _tap ??= new OneshotParameter("rhythm.tap", "Rhythm Tap");
            _syncBeat ??= new OneshotParameter("rhythm.sync-beat", "Rhythm Sync Beat");
            _syncBar ??= new OneshotParameter("rhythm.sync-bar", "Rhythm Sync Bar");
            _clock = new BpmClock(_bpm.Value);
            _bpm.Changed += QueueBpm;
            Debug.Log($"[BPM] Initialized at {Bpm:F2} BPM.", this);
        }

        private void OnDestroy()
        {
            if (_bpm != null) _bpm.Changed -= QueueBpm;
        }

        private void Update()
        {
            Clock.Update(UnityEngine.Time.unscaledDeltaTime);
            if (_tap.Value) TapTempo();
            if (_syncBeat.Value) SyncBeat();
            if (_syncBar.Value) SyncBar();
            SynchronizeBpmParameter();
        }

        public int BeatModulo(int count) => Clock.BeatModulo(count);
        public bool IsBeatModulo(int count, int target) => Clock.IsBeatModulo(count, target);

        public void SetBpm(double bpm)
        {
            _bpm.TrySetValue((float)bpm);
        }

        public void TapTempo()
        {
            _hasQueuedBpm = false;
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

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get
            {
                yield return _bpm;
                yield return _tap;
                yield return _syncBeat;
                yield return _syncBar;
            }
        }

        public Element CreateElement()
        {
            return UI.Column(
                UI.Label("Rhythm"),
                UI.Field("BPM", () => _bpm.Value, value => _bpm.TrySetValue(value)),
                UI.Row(
                    UI.Button("Tap", _tap.Trigger),
                    UI.Button("Sync Beat", _syncBeat.Trigger),
                    UI.Button("Sync Bar", _syncBar.Trigger)),
                UI.Label(() => $"Beat: {Beat:F3}"),
                UI.Label(() => $"Beat Floor: {BeatFloor}"));
        }

        private void QueueBpm(float bpm)
        {
            if (_synchronizingBpmParameter) return;
            Clock.SetBpm(bpm);
            _hasQueuedBpm = true;
            Debug.Log($"[BPM] BPM change queued: {bpm:F2}.", this);
        }

        private void SynchronizeBpmParameter()
        {
            if (_hasQueuedBpm)
            {
                if (!Mathf.Approximately(_bpm.Value, (float)Bpm)) return;
                _hasQueuedBpm = false;
            }

            if (Mathf.Approximately(_bpm.Value, (float)Bpm)) return;
            _synchronizingBpmParameter = true;
            _bpm.TrySetValue((float)Bpm);
            _synchronizingBpmParameter = false;
        }
    }
}
