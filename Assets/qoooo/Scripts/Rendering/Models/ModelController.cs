using System;
using System.Collections.Generic;
using qoooo.Foundation.Timing;
using qoooo.Foundation.Utilities;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Rendering.Models
{
    // Arrangementが公開したactive slot群へ、scale・motion・Renderer visibilityをbroadcastする。
    public class ModelController : MonoBehaviour, IMidiParameterContainer
    {
        [SerializeField] private FloatParameter _scale = new("model.scale", "Model Scale", 0.25f, 1.75f, 1f);

        [SerializeField]
        private RadioParameter _motion = new("model.motion", "Model Motion", new[] { "Still", "Spin", "Pulse" });

        [SerializeField]
        private OneshotParameter _resetTransform = new("model.reset-transform", "Reset Model Transform");

        [SerializeField] private BoolParameter _active = new("model.active", "Model Active", true);

        // CREATIVE AREA / 創作領域: beatからmotionへの反応をここで調整する。clockの所有者はBpmManagerだけ。
        [SerializeField] [Min(0.01f)] private float _spinBeatsPerRotation = 16f;
        [SerializeField] [Min(0.01f)] private float _pulseCycleBeats = 4f;
        [SerializeField] [Min(0.01f)] private float _pulseEaseBeats = 2f;
        private readonly List<GameObject> _activeSlots = new();
        private readonly Dictionary<GameObject, Quaternion> _initialRotations = new();
        private readonly Dictionary<GameObject, Vector3> _initialScales = new();
        private readonly List<Renderer> _renderers = new();
        private ModelArrangementController _arrangement;
        private IBpmSource _bpmSource;

        private ModelManager _modelManager;

        private void Awake()
        {
            _modelManager = GetComponent<ModelManager>();
            _arrangement = GetComponent<ModelArrangementController>();
            if (_modelManager != null)
            {
                var models = _modelManager.Models;
                if (models != null)
                    foreach (var model in models)
                        RegisterInitialTransform(model);

                if (_arrangement == null)
                    _modelManager.CurrentModelChanged += SetSingleModel;
            }

            if (_arrangement != null)
                _arrangement.ActiveSlotsChanged += SetActiveSlots;
        }

        private void Start()
        {
            if (_arrangement != null)
                SetActiveSlots(_arrangement.ActiveSlots);
            else
                SetSingleModel(_modelManager?.CurrentModel);
        }

        private void Update()
        {
            if (_activeSlots.Count == 0) return;
            var scale = _scale.Value;
            // ここでModel timeを蓄積しない。共有beatをRhythmでvisual valueへ変換する。
            var beat = (float)(_bpmSource?.Beat ?? 0d);
            var rhythmIndex = Rhythm.GetIndex(beat, _pulseCycleBeats);
            var rhythmPhase = Rhythm.GetPhase(beat, _pulseCycleBeats, _pulseEaseBeats);
            foreach (var slot in _activeSlots)
            {
                if (slot == null) continue;
                var slotScale = scale;
                var rotation = _initialRotations.TryGetValue(slot, out var initialRotation)
                    ? initialRotation
                    : slot.transform.localRotation;
                if (!_resetTransform.Value)

                    // important: モデルの動きを作成する、選択肢は_motionの宣言部の個数を調整すること->Enumに変更したほうがよさそうだ
                    switch (_motion.Value)
                    {
                        case 0:
                            break;
                        case 1:
                            slotScale = 1f + 0.5f * Rhythm.GetLerpNoise(beat, 8, 2);
                            break;
                        case 2:
                            var localRhythmPhase = Mathf.Clamp01(rhythmPhase - rhythmIndex * 2f);
                            slotScale *= 1f + 0.2f * Mathf.Sin(localRhythmPhase * Mathf.PI);
                            break;
                    }

                slot.transform.localRotation = rotation;
                ApplyScale(slot, slotScale);
            }

            SetRendererVisibility(_active.Value);
        }

        private void OnDestroy()
        {
            if (_modelManager != null)
                _modelManager.CurrentModelChanged -= SetSingleModel;
            if (_arrangement != null)
                _arrangement.ActiveSlotsChanged -= SetActiveSlots;
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

        [Inject]
        public void Construct(IBpmSource bpmSource)
        {
            _bpmSource = bpmSource;
        }

        private void SetRendererVisibility(bool visible)
        {
            foreach (var renderer in _renderers)
                if (renderer != null)
                    renderer.enabled = visible;
        }

        private void SetSingleModel(GameObject model)
        {
            SetActiveSlots(model == null ? Array.Empty<GameObject>() : new[] { model });
        }

        private void SetActiveSlots(IReadOnlyList<GameObject> slots)
        {
            _activeSlots.Clear();
            _renderers.Clear();
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                RegisterInitialTransform(slot);
                _activeSlots.Add(slot);
                _renderers.AddRange(slot.GetComponentsInChildren<Renderer>(true));
            }

            SetRendererVisibility(_active.Value);
        }

        private void RegisterInitialTransform(GameObject model)
        {
            if (model == null) return;
            _initialScales.TryAdd(model, model.transform.localScale);
            _initialRotations.TryAdd(model, model.transform.localRotation);
        }

        private void ApplyScale(GameObject slot, float multiplier)
        {
            if (slot != null && _initialScales.TryGetValue(slot, out var initialScale))
                slot.transform.localScale = initialScale * multiplier;
        }
    }
}