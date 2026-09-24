using System;
using System.Collections.Generic;
using System.Linq;
using PrefsGUI;
using qoooo.Foundation.Timing;
using qoooo.Foundation.Utilities;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Presentation.Contracts;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Rendering.Models
{
    // Arrangementが公開したbase transformへ、scale・animation・Renderer visibilityをbroadcastする。
    public class ModelController : MonoBehaviour, IMidiParameterContainer, IUiTarget, IPrefsSaveParticipant
    {
        private const string AnimationPresetsPrefsKey = "qoooo.model.animation-presets.v1";
        [SerializeField] private FloatParameter _scale = new("model.scale", "Model Scale", 0.25f, 1.75f, 1f);

        [SerializeField]
        private RadioParameter _motion = new("model.motion", "Model Motion", new[] { "Still", "Spin", "Pulse" });

        [SerializeField]
        private OneshotParameter _resetTransform = new("model.reset-transform", "Reset Model Transform");

        [SerializeField] private BoolParameter _active = new("model.active", "Model Active", true);
        [SerializeField] private BoolParameter _animationEnabled = new(
            "model.animation-enabled", "Animation Enabled", true);
        [SerializeField] private FloatParameter _animationSpeed = new(
            "model.animation-speed", "Animation Speed", 0f, 2f, 1f);
        [SerializeField] private FloatParameter _animationStrength = new(
            "model.animation-strength", "Animation Strength", 0f, 1f, 1f);
        [SerializeField] private RadioParameter _animationPreset = new(
            "model.animation-preset", "Animation Preset", new[] { "Default" });

        // CREATIVE AREA / 創作領域: beatからmotionへの反応をここで調整する。clockの所有者はBpmManagerだけ。
        [SerializeField] [Min(0.01f)] private float _pulseCycleBeats = 4f;
        [SerializeField] [Min(0.01f)] private float _pulseEaseBeats = 2f;
        [SerializeField] private List<ModelAnimationEntry> _animations = new();
        [SerializeField] private List<ModelAnimationPreset> _animationPresets = new();
        private readonly PrefsAny<ModelAnimationPresetRecord> _animationPresetPrefs = new(AnimationPresetsPrefsKey);
        private ModelAnimationPresetRecord _animationPresetRecordBeforePrepare;
        private readonly List<GameObject> _activeSlots = new();
        private readonly Dictionary<GameObject, Vector3> _basePositions = new();
        private readonly Dictionary<GameObject, Quaternion> _baseRotations = new();
        private readonly Dictionary<GameObject, Vector3> _baseScales = new();
        private readonly Dictionary<GameObject, Vector3> _visualCenters = new();
        private readonly List<Renderer> _renderers = new();
        private readonly Dictionary<string, bool> _animationFoldStates = new();
        private ModelArrangementController _arrangement;
        private IBpmSource _bpmSource;
        private int _animationTypeToAdd;
        private int _activeAnimationPreset = -1;
        private int _animationPresetRevision;
        private float _lastAnimationSpeed = float.NaN;
        private float _animationSecondsOffset;
        private float _animationBeatOffset;

        private ModelManager _modelManager;

        private void Awake()
        {
            LoadAnimationPresets();
            _modelManager = GetComponent<ModelManager>();
            _arrangement = GetComponent<ModelArrangementController>();
            if (_modelManager != null)
            {
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
            if (_activeAnimationPreset != _animationPreset.Value)
                ApplyAnimationPreset(_animationPreset.Value);
            if (_activeSlots.Count == 0) return;
            var scale = _scale.Value;
            // ここでModel timeを蓄積しない。共有beatをRhythmでvisual valueへ変換する。
            var beat = (float)(_bpmSource?.Beat ?? 0d);
            var rhythmIndex = Rhythm.GetIndex(beat, _pulseCycleBeats);
            var rhythmPhase = Rhythm.GetPhase(beat, _pulseCycleBeats, _pulseEaseBeats);
            var animationSeconds = GetScaledAnimationTime(Time.time, beat, false);
            var animationBeats = GetScaledAnimationTime(Time.time, beat, true);
            for (var slotIndex = 0; slotIndex < _activeSlots.Count; slotIndex++)
            {
                var slot = _activeSlots[slotIndex];
                if (slot == null) continue;
                var slotScale = scale;
                var positionOffset = Vector3.zero;
                var rotationOffset = Vector3.zero;
                if (!_resetTransform.Value)
                {
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

                    if (_animationEnabled.Value)
                        AccumulateAnimations(slotIndex, animationSeconds, animationBeats, _animationStrength.Value,
                            ref positionOffset, ref rotationOffset, ref slotScale);
                }

                ApplyTransform(slot, positionOffset, rotationOffset, slotScale);
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
                yield return _animationEnabled;
                yield return _animationSpeed;
                yield return _animationStrength;
                yield return _animationPreset;
            }
        }

        public Element CreateElement()
        {
            return UI.Fold("Model Animations",
                UI.DynamicElementOnStatusChanged(
                    AnimationPresetStatus,
                    _ => CreateAnimationPresetControls()),
                UI.Row(
                    UI.Dropdown("Add", () => _animationTypeToAdd,
                        value => _animationTypeToAdd = value, Enum.GetNames(typeof(ModelAnimationType))),
                    UI.Button("+ Add Animation", AddAnimation)),
                UI.DynamicElementOnStatusChanged(
                    AnimationListStatus,
                    _ => CreateAnimationList()))
                .Open();
        }

        public void PrepareSave()
        {
            _animationPresetRecordBeforePrepare = Copy(_animationPresetPrefs.Get());
            _animationPresetPrefs.Set(CaptureAnimationPresetRecord());
        }

        public void CommitSave()
        {
            _animationPresetRecordBeforePrepare = null;
            Debug.Log("[Model] Animation presets staged for Prefs.Save.", this);
        }

        public void AbortSave()
        {
            if (_animationPresetRecordBeforePrepare == null) return;
            _animationPresetPrefs.Set(_animationPresetRecordBeforePrepare);
            _animationPresetRecordBeforePrepare = null;
        }

        private Element CreateAnimationPresetControls()
        {
            return UI.Column(
                UI.Row(
                    UI.Dropdown("Preset", () => _animationPreset.Value,
                        SelectAnimationPreset, _animationPreset.Options.ToArray()),
                    UI.Field("Name", ActivePresetName, RenameActivePreset)),
                UI.Row(
                    UI.Button("New", CreateAnimationPreset),
                    UI.Button("Duplicate", DuplicateAnimationPreset),
                    UI.Button("Delete", DeleteAnimationPreset)));
        }

        private void LoadAnimationPresets()
        {
            _animationPreset ??= new RadioParameter(
                "model.animation-preset", "Animation Preset", new[] { "Default" });
            var record = _animationPresetPrefs.Get();
            if (record != null && record.HasSavedData && record.SchemaVersion == 1)
                _animationPresets = Copy(record.Presets) ?? new List<ModelAnimationPreset>();

            if (_animationPresets.Count == 0)
            {
                _animationPresets.Add(new ModelAnimationPreset
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = "Default",
                    Animations = _animations ?? new List<ModelAnimationEntry>()
                });
            }

            AddStarterAnimationPresets();
            EnsureAnimationPresets();
            var selectedId = record != null && record.HasSavedData ? record.ActivePresetId : null;
            SynchronizeAnimationPresetOptions(selectedId);
            ApplyAnimationPreset(_animationPreset.Value);
        }

        private void AddStarterAnimationPresets()
        {
            foreach (var preset in CreateStarterAnimationPresets())
            {
                if (_animationPresets.Count >= 8) break;
                if (_animationPresets.Any(value => value.Id == preset.Id)) continue;
                _animationPresets.Add(preset);
            }
        }

        private static IEnumerable<ModelAnimationPreset> CreateStarterAnimationPresets()
        {
            yield return Preset("starter-orbit-v1", "Orbit",
                Animation(ModelAnimationType.Rotate, ModelAnimationTiming.ContinuousBeats,
                    ModelAnimationPattern.Traverse, new Vector3(0f, 360f, 0f), 8f));
            yield return Preset("starter-bounce-v1", "Bounce",
                Animation(ModelAnimationType.Translate, ModelAnimationTiming.BeatCycle,
                    ModelAnimationPattern.Envelope, new Vector3(0f, 1.2f, 0f), 2f,
                    ModelAnimationEasing.EaseOutQuad, 0.25f,
                    new ModelAnimationEnvelope { Rise = 0.12f, HighHold = 0.08f, Fall = 0.3f, LowHold = 0.5f }));
            yield return Preset("starter-pulse-v1", "Pulse",
                ScaleAnimation(1f, 1.35f, 1f,
                    new ModelAnimationEnvelope { Rise = 0.08f, HighHold = 0.12f, Fall = 0.2f, LowHold = 0.6f }));
            yield return Preset("starter-drift-v1", "Drift",
                Animation(ModelAnimationType.Translate, ModelAnimationTiming.SecondsCycle,
                    ModelAnimationPattern.PingPong, new Vector3(1.2f, 0.35f, 0f), 6f,
                    ModelAnimationEasing.EaseInOutSine, 0.6f));
            yield return Preset("starter-twist-v1", "Twist",
                Animation(ModelAnimationType.Rotate, ModelAnimationTiming.BeatCycle,
                    ModelAnimationPattern.PingPong, new Vector3(20f, 180f, 10f), 4f,
                    ModelAnimationEasing.EaseInOutSine, 0.35f));
            yield return Preset("starter-float-v1", "Float",
                Animation(ModelAnimationType.Translate, ModelAnimationTiming.SecondsCycle,
                    ModelAnimationPattern.PingPong, new Vector3(0f, 0.75f, 0f), 4f,
                    ModelAnimationEasing.SmoothStep, 1f));
            yield return Preset("starter-impact-v1", "Impact",
                ScaleAnimation(0.9f, 1.45f, 2f,
                    new ModelAnimationEnvelope { Rise = 0.05f, HighHold = 0.1f, Fall = 0.2f, LowHold = 0.65f }),
                Animation(ModelAnimationType.Rotate, ModelAnimationTiming.BeatCycle,
                    ModelAnimationPattern.Envelope, new Vector3(0f, 45f, 0f), 2f,
                    ModelAnimationEasing.EaseOutQuad, 0.5f,
                    new ModelAnimationEnvelope { Rise = 0.05f, HighHold = 0.1f, Fall = 0.25f, LowHold = 0.6f }));
        }

        private static ModelAnimationPreset Preset(string id, string name, params ModelAnimationEntry[] animations)
        {
            return new ModelAnimationPreset
            {
                Id = id,
                Name = name,
                Animations = animations.ToList()
            };
        }

        private static ModelAnimationEntry Animation(
            ModelAnimationType type,
            ModelAnimationTiming timing,
            ModelAnimationPattern pattern,
            Vector3 amount,
            float cycle,
            ModelAnimationEasing easing = ModelAnimationEasing.SmoothStep,
            float randomPhase = 0f,
            ModelAnimationEnvelope envelope = null)
        {
            return new ModelAnimationEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = type,
                Timing = timing,
                Pattern = pattern,
                Easing = easing,
                SecondsPerCycle = cycle,
                BeatsPerCycle = cycle,
                Amount = amount,
                Envelope = envelope ?? new ModelAnimationEnvelope(),
                Seed = 23,
                RandomPhase = randomPhase
            };
        }

        private static ModelAnimationEntry ScaleAnimation(
            float from,
            float to,
            float beats,
            ModelAnimationEnvelope envelope)
        {
            var animation = Animation(ModelAnimationType.Scale, ModelAnimationTiming.BeatCycle,
                ModelAnimationPattern.Envelope, Vector3.zero, beats, ModelAnimationEasing.EaseOutQuad,
                0.2f, envelope);
            animation.FromScale = from;
            animation.ToScale = to;
            return animation;
        }

        private void EnsureAnimationPresets()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var preset in _animationPresets)
            {
                if (string.IsNullOrEmpty(preset.Id)) preset.Id = Guid.NewGuid().ToString("N");
                preset.Name = UniqueAnimationPresetName(preset.Name, names);
                names.Add(preset.Name);
                preset.Animations ??= new List<ModelAnimationEntry>();
                foreach (var animation in preset.Animations)
                    if (animation != null)
                        EnsureAnimation(animation);
            }
        }

        private void SynchronizeAnimationPresetOptions(string selectedId)
        {
            _animationPreset.ReplaceOptions(_animationPresets.Select(preset => preset.Name));
            var index = _animationPresets.FindIndex(preset => preset.Id == selectedId);
            _animationPreset.TrySetValue(index >= 0 ? index : Mathf.Clamp(_animationPreset.Value, 0, _animationPresets.Count - 1));
            _animationPresetRevision++;
        }

        private void SelectAnimationPreset(int index)
        {
            if (!_animationPreset.TrySetValue(index)) return;
            ApplyAnimationPreset(_animationPreset.Value);
            _animationPresetRevision++;
        }

        private void ApplyAnimationPreset(int index)
        {
            if (_animationPresets.Count == 0) return;
            _activeAnimationPreset = Mathf.Clamp(index, 0, _animationPresets.Count - 1);
            _animations = _animationPresets[_activeAnimationPreset].Animations;
            _animationFoldStates.Clear();
        }

        private string ActivePresetName()
        {
            return _animationPresets.Count == 0 ? string.Empty : _animationPresets[_animationPreset.Value].Name;
        }

        private void RenameActivePreset(string name)
        {
            if (_animationPresets.Count == 0) return;
            var preset = _animationPresets[_animationPreset.Value];
            var selectedId = preset.Id;
            preset.Name = UniqueAnimationPresetName(name,
                new HashSet<string>(_animationPresets.Where(value => value != preset).Select(value => value.Name),
                    StringComparer.Ordinal));
            SynchronizeAnimationPresetOptions(selectedId);
        }

        private void CreateAnimationPreset()
        {
            var preset = new ModelAnimationPreset
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = UniqueAnimationPresetName("New Preset",
                    new HashSet<string>(_animationPresets.Select(value => value.Name), StringComparer.Ordinal))
            };
            _animationPresets.Add(preset);
            SynchronizeAnimationPresetOptions(preset.Id);
            ApplyAnimationPreset(_animationPreset.Value);
        }

        private void DuplicateAnimationPreset()
        {
            if (_animationPresets.Count == 0) return;
            var source = _animationPresets[_animationPreset.Value];
            var preset = Copy(source);
            preset.Id = Guid.NewGuid().ToString("N");
            preset.Name = UniqueAnimationPresetName($"{source.Name} Copy",
                new HashSet<string>(_animationPresets.Select(value => value.Name), StringComparer.Ordinal));
            foreach (var animation in preset.Animations)
                animation.Id = Guid.NewGuid().ToString("N");
            _animationPresets.Insert(_animationPreset.Value + 1, preset);
            SynchronizeAnimationPresetOptions(preset.Id);
            ApplyAnimationPreset(_animationPreset.Value);
        }

        private void DeleteAnimationPreset()
        {
            if (_animationPresets.Count <= 1) return;
            var index = _animationPreset.Value;
            _animationPresets.RemoveAt(index);
            var selectedId = _animationPresets[Mathf.Clamp(index, 0, _animationPresets.Count - 1)].Id;
            SynchronizeAnimationPresetOptions(selectedId);
            ApplyAnimationPreset(_animationPreset.Value);
        }

        private ModelAnimationPresetRecord CaptureAnimationPresetRecord()
        {
            return new ModelAnimationPresetRecord
            {
                HasSavedData = true,
                ActivePresetId = _animationPresets[_animationPreset.Value].Id,
                Presets = Copy(_animationPresets)
            };
        }

        private string AnimationPresetStatus()
        {
            return $"{_animationPreset.Value}:{_animationPresetRevision}";
        }

        private static string UniqueAnimationPresetName(string value, ISet<string> names)
        {
            var baseName = string.IsNullOrWhiteSpace(value) ? "Untitled" : value.Trim();
            var candidate = baseName;
            for (var suffix = 2; names.Contains(candidate); suffix++)
                candidate = $"{baseName} {suffix}";
            return candidate;
        }

        private static ModelAnimationPreset Copy(ModelAnimationPreset value)
        {
            return value == null ? null : JsonUtility.FromJson<ModelAnimationPreset>(JsonUtility.ToJson(value));
        }

        private static List<ModelAnimationPreset> Copy(List<ModelAnimationPreset> value)
        {
            if (value == null) return null;
            return JsonUtility.FromJson<ModelAnimationPresetRecord>(JsonUtility.ToJson(
                new ModelAnimationPresetRecord { Presets = value })).Presets;
        }

        private static ModelAnimationPresetRecord Copy(ModelAnimationPresetRecord value)
        {
            return value == null
                ? null
                : JsonUtility.FromJson<ModelAnimationPresetRecord>(JsonUtility.ToJson(value));
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
                CaptureBaseTransform(slot);
                _activeSlots.Add(slot);
                _renderers.AddRange(slot.GetComponentsInChildren<Renderer>(true));
            }

            SetRendererVisibility(_active.Value);
        }

        private void CaptureBaseTransform(GameObject model)
        {
            if (model == null) return;
            _basePositions[model] = model.transform.localPosition;
            _baseRotations[model] = model.transform.localRotation;
            _baseScales[model] = model.transform.localScale;
            _visualCenters[model] = CalculateVisualCenter(model);
        }

        private static Vector3 CalculateVisualCenter(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var bounds = new Bounds();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var localBounds = renderer.localBounds;
                var rendererToModel = model.transform.worldToLocalMatrix * renderer.localToWorldMatrix;
                var min = localBounds.min;
                var max = localBounds.max;
                for (var corner = 0; corner < 8; corner++)
                {
                    var point = rendererToModel.MultiplyPoint3x4(new Vector3(
                        (corner & 1) == 0 ? min.x : max.x,
                        (corner & 2) == 0 ? min.y : max.y,
                        (corner & 4) == 0 ? min.z : max.z));
                    if (hasBounds) bounds.Encapsulate(point);
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                }
            }

            return hasBounds ? bounds.center : Vector3.zero;
        }

        private void AccumulateAnimations(
            int slotIndex,
            float animationSeconds,
            float animationBeats,
            float strength,
            ref Vector3 positionOffset,
            ref Vector3 rotationOffset,
            ref float scaleMultiplier)
        {
            foreach (var animation in _animations)
            {
                if (animation == null || !animation.Enabled) continue;
                var value = EvaluateAnimation(animation, slotIndex, animationSeconds, animationBeats);
                switch (animation.Type)
                {
                    case ModelAnimationType.Translate:
                        positionOffset += animation.Amount * value * strength;
                        break;
                    case ModelAnimationType.Rotate:
                        rotationOffset += animation.Amount * value * strength;
                        break;
                    case ModelAnimationType.Scale:
                        var evaluatedMultiplier = Mathf.LerpUnclamped(animation.FromScale, animation.ToScale, value);
                        scaleMultiplier *= Mathf.Lerp(1f, evaluatedMultiplier, strength);
                        break;
                }
            }
        }

        private float GetScaledAnimationTime(float seconds, float beats, bool beatTiming)
        {
            var speed = _animationSpeed.Value;
            if (!Mathf.Approximately(speed, _lastAnimationSpeed))
            {
                if (!float.IsNaN(_lastAnimationSpeed))
                {
                    _animationSecondsOffset += seconds * (_lastAnimationSpeed - speed);
                    _animationBeatOffset += beats * (_lastAnimationSpeed - speed);
                }

                _lastAnimationSpeed = speed;
            }

            return beatTiming
                ? beats * speed + _animationBeatOffset
                : seconds * speed + _animationSecondsOffset;
        }

        private static float EvaluateAnimation(
            ModelAnimationEntry animation,
            int slotIndex,
            float animationSeconds,
            float animationBeats)
        {
            var duration = animation.Timing is ModelAnimationTiming.ContinuousBeats or ModelAnimationTiming.BeatCycle
                ? Mathf.Max(0.01f, animation.BeatsPerCycle)
                : Mathf.Max(0.01f, animation.SecondsPerCycle);
            var time = animation.Timing is ModelAnimationTiming.ContinuousBeats or ModelAnimationTiming.BeatCycle
                ? animationBeats
                : animationSeconds;
            var progress = time / duration;
            progress += Pcg.Pcg01(new UInt2(animation.Seed, (uint)slotIndex)).x
                        * Mathf.Clamp01(animation.RandomPhase);

            if (animation.Timing is ModelAnimationTiming.SecondsCycle or ModelAnimationTiming.BeatCycle)
                progress = animation.Loop ? Mathf.Repeat(progress, 1f) : Mathf.Clamp01(progress);

            var value = animation.Pattern switch
            {
                ModelAnimationPattern.Traverse => EaseProgress(progress, animation.Easing),
                ModelAnimationPattern.PingPong => ApplyEasing(Mathf.PingPong(progress, 1f), animation.Easing),
                ModelAnimationPattern.Envelope => EvaluateEnvelope(animation.Envelope, progress, animation.Easing),
                _ => progress
            };
            return value;
        }

        private static float EaseProgress(float progress, ModelAnimationEasing easing)
        {
            var cycle = Mathf.Floor(progress);
            return cycle + ApplyEasing(progress - cycle, easing);
        }

        private static float EvaluateEnvelope(
            ModelAnimationEnvelope envelope,
            float progress,
            ModelAnimationEasing easing)
        {
            envelope ??= new ModelAnimationEnvelope();
            var rise = Mathf.Max(0f, envelope.Rise);
            var highHold = Mathf.Max(0f, envelope.HighHold);
            var fall = Mathf.Max(0f, envelope.Fall);
            var lowHold = Mathf.Max(0f, envelope.LowHold);
            var duration = Mathf.Max(0.01f, rise + highHold + fall + lowHold);
            var time = Mathf.Repeat(progress, 1f) * duration;
            if (time < rise)
                return ApplyEasing(time / Mathf.Max(0.01f, rise), easing);
            time -= rise;
            if (time < highHold) return 1f;
            time -= highHold;
            if (time < fall)
                return 1f - ApplyEasing(time / Mathf.Max(0.01f, fall), easing);
            return 0f;
        }

        private static float ApplyEasing(float value, ModelAnimationEasing easing)
        {
            value = Mathf.Clamp01(value);
            return easing switch
            {
                ModelAnimationEasing.Linear => value,
                ModelAnimationEasing.SmoothStep => value * value * (3f - 2f * value),
                ModelAnimationEasing.EaseInSine => Easing.EaseInSine(value),
                ModelAnimationEasing.EaseOutSine => Easing.EaseOutSine(value),
                ModelAnimationEasing.EaseInOutSine => Easing.EaseInOutSine(value),
                ModelAnimationEasing.EaseInQuad => Easing.EaseInQuad(value),
                ModelAnimationEasing.EaseOutQuad => Easing.EaseOutQuad(value),
                ModelAnimationEasing.EaseInOutQuad => Easing.EaseInOutQuad(value),
                _ => value
            };
        }

        private void ApplyTransform(
            GameObject slot,
            Vector3 positionOffset,
            Vector3 rotationOffset,
            float scaleMultiplier)
        {
            if (!_basePositions.TryGetValue(slot, out var basePosition)
                || !_baseRotations.TryGetValue(slot, out var baseRotation)
                || !_baseScales.TryGetValue(slot, out var baseScale)
                || !_visualCenters.TryGetValue(slot, out var visualCenter)) return;
            var rotation = baseRotation * Quaternion.Euler(rotationOffset);
            var scale = baseScale * scaleMultiplier;
            slot.transform.localPosition = basePosition + positionOffset
                                           - rotation * Vector3.Scale(visualCenter, scale);
            slot.transform.localRotation = rotation;
            slot.transform.localScale = scale;
        }

        private Element CreateAnimationList()
        {
            if (_animations.Count == 0) return UI.HelpBox("No model animations.");
            var elements = new Element[_animations.Count];
            for (var index = 0; index < _animations.Count; index++)
            {
                var animation = _animations[index];
                EnsureAnimation(animation);
                var fold = UI.Fold($"{index + 1}. {animation.Type}", CreateAnimationInspector(animation));
                fold.IsOpen = _animationFoldStates.TryGetValue(animation.Id, out var isOpen) && isOpen;
                elements[index] = fold;
            }
            return UI.Column(elements);
        }

        private Element CreateAnimationInspector(ModelAnimationEntry animation)
        {
            return UI.Column(
                UI.Field("Enabled", () => animation.Enabled, value => animation.Enabled = value),
                UI.Dropdown("Type", () => (int)animation.Type,
                    value => animation.Type = (ModelAnimationType)value, Enum.GetNames(typeof(ModelAnimationType))),
                UI.Dropdown("Timing", () => (int)animation.Timing,
                    value => animation.Timing = (ModelAnimationTiming)value, Enum.GetNames(typeof(ModelAnimationTiming))),
                UI.Dropdown("Pattern", () => (int)animation.Pattern,
                    value => animation.Pattern = (ModelAnimationPattern)value, Enum.GetNames(typeof(ModelAnimationPattern))),
                UI.Dropdown("Easing", () => (int)animation.Easing,
                    value => animation.Easing = (ModelAnimationEasing)value, Enum.GetNames(typeof(ModelAnimationEasing))),
                UI.DynamicElementOnStatusChanged(
                    () => animation.Type,
                    _ => CreateAmountFields(animation)),
                UI.DynamicElementOnStatusChanged(
                    () => animation.Timing,
                    _ => CreateTimingFields(animation)),
                UI.DynamicElementOnStatusChanged(
                    () => animation.Pattern,
                    _ => CreatePatternFields(animation)),
                UI.Field("Seed", () => animation.Seed, value => animation.Seed = value),
                UI.Slider("Random Phase", () => animation.RandomPhase,
                    value => animation.RandomPhase = value, 0f, 1f),
                UI.Row(
                    UI.Button("Duplicate", () => DuplicateAnimation(animation.Id)),
                    UI.Button("Delete", () => DeleteAnimation(animation.Id))));
        }

        private static Element CreateAmountFields(ModelAnimationEntry animation)
        {
            return animation.Type == ModelAnimationType.Scale
                ? UI.Column(
                    UI.Field("From Scale", () => animation.FromScale, value => animation.FromScale = value),
                    UI.Field("To Scale", () => animation.ToScale, value => animation.ToScale = value))
                : UI.Field("Amount", () => animation.Amount, value => animation.Amount = value);
        }

        private static Element CreateTimingFields(ModelAnimationEntry animation)
        {
            return animation.Timing switch
            {
                ModelAnimationTiming.ContinuousSeconds =>
                    UI.Field("Seconds Per Unit", () => animation.SecondsPerCycle,
                        value => animation.SecondsPerCycle = Mathf.Max(0.01f, value)),
                ModelAnimationTiming.ContinuousBeats =>
                    UI.Field("Beats Per Unit", () => animation.BeatsPerCycle,
                        value => animation.BeatsPerCycle = Mathf.Max(0.01f, value)),
                ModelAnimationTiming.SecondsCycle => UI.Column(
                    UI.Field("Seconds Per Cycle", () => animation.SecondsPerCycle,
                        value => animation.SecondsPerCycle = Mathf.Max(0.01f, value)),
                    UI.Field("Loop", () => animation.Loop, value => animation.Loop = value)),
                ModelAnimationTiming.BeatCycle => UI.Column(
                    UI.Field("Beats Per Cycle", () => animation.BeatsPerCycle,
                        value => animation.BeatsPerCycle = Mathf.Max(0.01f, value)),
                    UI.Field("Loop", () => animation.Loop, value => animation.Loop = value)),
                _ => UI.Space()
            };
        }

        private static Element CreatePatternFields(ModelAnimationEntry animation)
        {
            if (animation.Pattern != ModelAnimationPattern.Envelope) return UI.Space();
            animation.Envelope ??= new ModelAnimationEnvelope();
            return UI.Column(
                UI.Field("Rise", () => animation.Envelope.Rise,
                    value => animation.Envelope.Rise = Mathf.Max(0f, value)),
                UI.Field("High Hold", () => animation.Envelope.HighHold,
                    value => animation.Envelope.HighHold = Mathf.Max(0f, value)),
                UI.Field("Fall", () => animation.Envelope.Fall,
                    value => animation.Envelope.Fall = Mathf.Max(0f, value)),
                UI.Field("Low Hold", () => animation.Envelope.LowHold,
                    value => animation.Envelope.LowHold = Mathf.Max(0f, value)));
        }

        private void AddAnimation()
        {
            var animation = new ModelAnimationEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = (ModelAnimationType)_animationTypeToAdd
            };
            _animations.Add(animation);
            _animationFoldStates[animation.Id] = true;
        }

        private void DuplicateAnimation(string id)
        {
            var index = _animations.FindIndex(animation => animation?.Id == id);
            if (index < 0) return;
            var duplicate = JsonUtility.FromJson<ModelAnimationEntry>(
                JsonUtility.ToJson(_animations[index]));
            duplicate.Id = Guid.NewGuid().ToString("N");
            _animations.Insert(index + 1, duplicate);
            _animationFoldStates[duplicate.Id] = true;
        }

        private void DeleteAnimation(string id)
        {
            var index = _animations.FindIndex(animation => animation?.Id == id);
            if (index < 0) return;
            _animations.RemoveAt(index);
            _animationFoldStates.Remove(id);
        }

        private int AnimationListStatus()
        {
            unchecked
            {
                var hash = _animations.Count;
                foreach (var animation in _animations)
                    hash = hash * 31 + (animation?.Id?.GetHashCode() ?? 0);
                return hash;
            }
        }

        private static void EnsureAnimation(ModelAnimationEntry animation)
        {
            if (string.IsNullOrEmpty(animation.Id)) animation.Id = Guid.NewGuid().ToString("N");
            animation.Envelope ??= new ModelAnimationEnvelope();
        }
    }
}
