using qoooo.Foundation.Timing;
using qoooo.Parameters.Model;
using UnityEngine;

namespace qoooo.Parameters.Runtime
{
    [DefaultExecutionOrder(-110)]
    public sealed class MidiParameterRuntime : MonoBehaviour
    {
        [SerializeField] private MidiParameterRegistry _registry;
        private IBpmSource _bpmSource;
        public void Configure(MidiParameterRegistry registry, IBpmSource bpmSource) { _registry=registry;_bpmSource=bpmSource; }
        private void Update()
        {
            if (_registry == null) return;
            if (_bpmSource == null) return;
            var beat = _bpmSource.Beat;
            foreach(var p in _registry.Parameters)
            {
                if (p is MomentaryParameter momentary) momentary.BeginFrame();
                else if (p is OneshotParameter oneshot) oneshot.BeginFrame();
                else if (p is SequenceParameter sequence) sequence.UpdateBeat(beat);
            }
        }
    }
}
