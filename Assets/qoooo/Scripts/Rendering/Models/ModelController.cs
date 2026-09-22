using System.Collections.Generic;
using qoooo.Foundation.Timing;
using qoooo.Foundation.Utilities;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Rendering.Models
{
    public class ModelController : MonoBehaviour, IMidiParameterContainer
    {
        [SerializeField] private FloatParameter _scale = new("model.scale", "Model Scale", 0.25f, 1.75f, 1f);

        [SerializeField]
        private RadioParameter _motion = new("model.motion", "Model Motion", new[] { "Still", "Spin", "Pulse" });

        [SerializeField]
        private OneshotParameter _resetTransform = new("model.reset-transform", "Reset Model Transform");

        [SerializeField]
        private BoolParameter _active = new("model.active", "Model Active", true);

        // CREATIVE AREA: tune beat-to-motion response here. BpmManager remains the only clock owner.
        [SerializeField, Min(0.01f)] private float _spinBeatsPerRotation = 16f;
        [SerializeField, Min(0.01f)] private float _pulseCycleBeats = 4f;
        [SerializeField, Min(0.01f)] private float _pulseEaseBeats = 2f;

        private ModelManager _modelManager;
        private IBpmSource _bpmSource;
        private GameObject _currentModel;
        private Renderer[] _renderers = System.Array.Empty<Renderer>();
        private readonly Dictionary<GameObject, Vector3> _initialScales = new();

        private void Awake()
        {
            _modelManager = GetComponent<ModelManager>();
            if (_modelManager != null)
            {
                var models = _modelManager.Models;
                if (models != null)
                    foreach (var model in models)
                        if (model != null)
                            _initialScales[model] = model.transform.localScale;

                _modelManager.CurrentModelChanged += SetCurrentModel;
            }
        }

        private void Start()
        {
            SetCurrentModel(_modelManager?.CurrentModel);
        }

        [Inject]
        public void Construct(IBpmSource bpmSource)
        {
            _bpmSource = bpmSource;
        }

        private void OnDestroy()
        {
            if (_modelManager != null)
                _modelManager.CurrentModelChanged -= SetCurrentModel;
        }

        private void Update()
        {
            if (_currentModel == null) return;
            if (_resetTransform.Value) _currentModel.transform.localRotation = Quaternion.identity;
            var scale = _scale.Value;
            // Do not accumulate model time here. Convert the shared beat into visual values with Rhythm.
            var beat = (float)(_bpmSource?.Beat ?? 0d);
            var rhythmIndex = Rhythm.GetIndex(beat, _pulseCycleBeats);
            var rhythmPhase = Rhythm.GetPhase(beat, _pulseCycleBeats, _pulseEaseBeats);
            switch (_motion.Value)
            {
                case 1:
                    _currentModel.transform.localRotation = Quaternion.Euler(
                        0f,
                        beat * 360f / _spinBeatsPerRotation,
                        0f);
                    break;
                case 2:
                    var localRhythmPhase = Mathf.Clamp01(rhythmPhase - rhythmIndex * 2f);
                    var pulse = Mathf.Sin(localRhythmPhase * Mathf.PI);
                    scale *= 1f + 0.2f * pulse;
                    break;
            }

            ApplyScale(scale);
            SetRendererVisibility(_active.Value);
        }

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get
            {
                yield return _scale;
                yield return _motion;
                yield return _resetTransform;
                yield return _active;
            }
        }

        private void SetRendererVisibility(bool visible)
        {
            foreach (var renderer in _renderers)
                if (renderer != null)
                    renderer.enabled = visible;
        }

        private void SetCurrentModel(GameObject model)
        {
            _currentModel = model;
            _renderers = _currentModel == null
                ? System.Array.Empty<Renderer>()
                : _currentModel.GetComponentsInChildren<Renderer>(true);
            ApplyScale(_scale.Value);
            SetRendererVisibility(_active.Value);
        }

        private void ApplyScale(float multiplier)
        {
            if (_currentModel != null && _initialScales.TryGetValue(_currentModel, out var initialScale))
                _currentModel.transform.localScale = initialScale * multiplier;
        }
    }
}
