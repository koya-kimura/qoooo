using System;
using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Presentation.Contracts;
using RosettaUI;
using UnityEngine;

namespace qoooo.Rendering.Models
{
    public enum ModelArrangementMode
    {
        Single,
        Row,
        Column,
        Grid
    }

    public enum ModelSourceMode
    {
        Same,
        Sequence
    }

    // OWNER / Arrangement state: 選択中sourceを起点にslot群の配置とsource割当を管理する。
    // WHY / Poster上のTexture repeatではなく、Model Cameraに映る3D object配置を扱う。
    [DefaultExecutionOrder(-9100)]
    public sealed class ModelArrangementController : MonoBehaviour, IUiTarget, IMidiParameterContainer
    {
        private const int MaximumAxisCount = 4;
        private const int MaximumSlotCount = 16;
        private static readonly string[] ArrangementModeNames = { "Single", "Row", "Column", "Grid" };
        private static readonly string[] CountNames = { "1", "2", "3", "4" };

        [SerializeField] private ModelArrangementMode _mode;
        [SerializeField] private Vector3Int _counts = Vector3Int.one;
        [SerializeField] private Vector3 _spacings = Vector3.one * 0.25f;
        [SerializeField] private ModelSourceMode _sourceMode;
        [SerializeField, HideInInspector, Range(1, MaximumAxisCount)] private int _sideCount = 1;
        [SerializeField, HideInInspector, Min(0f)] private float _spacing = 0.25f;
        [SerializeField, HideInInspector] private bool _xyzArrangementMigrated;
        [SerializeField] private RadioParameter _arrangementMode = new(
            "model.arrangement-mode", "Arrangement Mode", ArrangementModeNames);
        [SerializeField] private RadioParameter _countX = new("model.count-x", "Count X", CountNames);
        [SerializeField] private RadioParameter _countY = new("model.count-y", "Count Y", CountNames);
        [SerializeField] private RadioParameter _countZ = new("model.count-z", "Count Z", CountNames);

        private readonly List<GameObject> _activeSlots = new();
        private readonly Dictionary<GameObject, Vector3> _sourcePositions = new();
        private readonly Dictionary<GameObject, Vector3> _sourceScales = new();
        private readonly Dictionary<GameObject, Quaternion> _sourceRotations = new();
        private ModelManager _modelManager;
        private ModelInstanceCache _instanceCache;
        private GameObject _prototype;
        private Vector3 _prototypePosition;
        private Vector3 _prototypeScale;
        private Quaternion _prototypeRotation;

        public IReadOnlyList<GameObject> ActiveSlots => _activeSlots;
        public event Action<IReadOnlyList<GameObject>> ActiveSlotsChanged;

        private void Awake()
        {
            MigrateArrangementSettings();
            InitializeParameters();
            _instanceCache = new ModelInstanceCache(transform, SourceScale, SourceRotation);
            _modelManager = GetComponent<ModelManager>();
            if (_modelManager != null)
            {
                foreach (var model in _modelManager.Models)
                    if (model != null)
                    {
                        _sourcePositions[model] = model.transform.localPosition;
                        _sourceScales[model] = model.transform.localScale;
                        _sourceRotations[model] = model.transform.localRotation;
                    }

                _modelManager.CurrentModelChanged += SetSource;
            }
        }

        private void Start()
        {
            // ModelManager.Awakeで確定したsourceを起点に、slot構成を初期化する。
            SetSource(_modelManager?.CurrentModel);
            PrewarmCurrentSource();
        }

        private void Update()
        {
            ApplyParameters();
        }

        private void OnDestroy()
        {
            if (_modelManager != null)
                _modelManager.CurrentModelChanged -= SetSource;

            RestorePrototypeTransform();
            _instanceCache?.Dispose();
        }

        public Element CreateElement()
        {
            return UI.Fold("Arrangement",
                UI.Dropdown("Mode", () => _arrangementMode.Value,
                    value => _arrangementMode.TrySetValue(value), ArrangementModeNames),
                UI.DynamicElementOnStatusChanged(
                    () => (ModelArrangementMode)_arrangementMode.Value,
                    CreateArrangementFields),
                UI.Dropdown("Source Mode", () => (int)_sourceMode, SetSourceMode,
                    Enum.GetNames(typeof(ModelSourceMode))))
                .Open();
        }

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get
            {
                yield return _arrangementMode;
                yield return _countX;
                yield return _countY;
                yield return _countZ;
            }
        }

        private Element CreateArrangementFields(ModelArrangementMode mode)
        {
            return mode switch
            {
                ModelArrangementMode.Row => UI.Column(
                    CreateCountDropdown("Count X", _countX),
                    CreateSpacingField("Spacing X", 0)),
                ModelArrangementMode.Column => UI.Column(
                    CreateCountDropdown("Count Y", _countY),
                    CreateSpacingField("Spacing Y", 1)),
                ModelArrangementMode.Grid => UI.Column(
                    CreateCountDropdown("Count X", _countX),
                    CreateCountDropdown("Count Y", _countY),
                    CreateCountDropdown("Count Z", _countZ),
                    CreateSpacingField("Spacing X", 0),
                    CreateSpacingField("Spacing Y", 1),
                    CreateSpacingField("Spacing Z", 2)),
                _ => UI.Space()
            };
        }

        private static Element CreateCountDropdown(string label, RadioParameter parameter)
        {
            return UI.Dropdown(label, () => parameter.Value, value => parameter.TrySetValue(value), CountNames);
        }

        private Element CreateSpacingField(string label, int axis)
        {
            return UI.Field(label, () => GetAxis(_spacings, axis), value => SetSpacing(axis, value));
        }

        private void SetSource(GameObject source)
        {
            if (_prototype == source)
            {
                RefreshSlots();
                return;
            }

            RestorePrototypeTransform();
            _prototype = source;
            _prototypePosition = SourcePosition(source);
            _prototypeScale = SourceScale(source);
            _prototypeRotation = SourceRotation(source);
            RefreshSlots();
        }

        private void InitializeParameters()
        {
            _arrangementMode ??= new RadioParameter("model.arrangement-mode", "Arrangement Mode", ArrangementModeNames);
            _countX ??= new RadioParameter("model.count-x", "Count X", CountNames);
            _countY ??= new RadioParameter("model.count-y", "Count Y", CountNames);
            _countZ ??= new RadioParameter("model.count-z", "Count Z", CountNames);

            _arrangementMode.ReplaceOptions(ArrangementModeNames, (int)_mode);
            _countX.ReplaceOptions(CountNames, _counts.x - 1);
            _countY.ReplaceOptions(CountNames, _counts.y - 1);
            _countZ.ReplaceOptions(CountNames, _counts.z - 1);
            _arrangementMode.ResetToDefault();
            _countX.ResetToDefault();
            _countY.ResetToDefault();
            _countZ.ResetToDefault();
        }

        private void ApplyParameters()
        {
            var nextMode = (ModelArrangementMode)Mathf.Clamp(_arrangementMode.Value, 0, ArrangementModeNames.Length - 1);
            var nextCounts = nextMode != _mode
                ? DefaultCounts(nextMode)
                : ClampCounts(new Vector3Int(
                    _countX.Value + 1,
                    _countY.Value + 1,
                    _countZ.Value + 1));

            // MIDI入力の組合せが16 slotsを超えた場合は、actual stateとParameter表示を同じ安全値へ戻す。
            _countX.TrySetValue(nextCounts.x - 1);
            _countY.TrySetValue(nextCounts.y - 1);
            _countZ.TrySetValue(nextCounts.z - 1);
            if (_mode == nextMode && _counts == nextCounts) return;

            _mode = nextMode;
            _counts = nextCounts;
            RefreshSlots();
        }

        private static Vector3Int DefaultCounts(ModelArrangementMode mode)
        {
            return mode switch
            {
                ModelArrangementMode.Row => new Vector3Int(4, 1, 1),
                ModelArrangementMode.Column => new Vector3Int(1, 4, 1),
                ModelArrangementMode.Grid => new Vector3Int(2, 2, 1),
                _ => Vector3Int.one
            };
        }

        private void SetMode(int value)
        {
            _mode = (ModelArrangementMode)Mathf.Clamp(value, 0, Enum.GetValues(typeof(ModelArrangementMode)).Length - 1);
            RefreshSlots();
        }

        private void SetCount(int axis, int value)
        {
            var counts = _counts;
            SetAxis(ref counts, axis, Mathf.Clamp(value, 1, MaximumAxisCount));
            var descriptor = CreateDescriptor(_mode, counts, _spacings);
            var otherAxes = Mathf.Max(1, descriptor.Count / GetAxis(descriptor.Counts, axis));
            SetAxis(ref counts, axis, Mathf.Min(GetAxis(counts, axis), MaximumSlotCount / otherAxes));
            _counts = counts;
            RefreshSlots();
        }

        private void SetSpacing(int axis, float value)
        {
            var spacings = _spacings;
            SetAxis(ref spacings, axis, Mathf.Max(0f, value));
            _spacings = spacings;
            RefreshSlots();
        }

        private void SetSourceMode(int value)
        {
            _sourceMode = (ModelSourceMode)Mathf.Clamp(value, 0, Enum.GetValues(typeof(ModelSourceMode)).Length - 1);
            RefreshSlots();
        }

        // FLOW / source -> runtime slots -> 1 Model Camera。sourceが変わったslotだけcloneを更新する。
        private void RefreshSlots()
        {
            if (_prototype == null)
            {
                _activeSlots.Clear();
                ActiveSlotsChanged?.Invoke(_activeSlots);
                return;
            }

            _activeSlots.Clear();
            // slot 0はScene上のoriginal、slot 1以降はCacheが所有するruntime clone。
            _prototype.SetActive(true);
            _activeSlots.Add(_prototype);
            var descriptor = CreateDescriptor(_mode, _counts, _spacings);
            var slotCount = descriptor.Count;
            for (var index = 1; index < slotCount; index++)
            {
                var instance = _instanceCache.GetOrCreate(index - 1, ResolveSlotSource(index));
                if (instance == null) continue;

                _activeSlots.Add(instance);
            }

            _instanceCache.SetActiveCount(slotCount - 1);

            for (var index = 0; index < _activeSlots.Count; index++)
            {
                var slot = _activeSlots[index];
                if (slot == null) continue;

                var source = index == 0 ? _prototype : ResolveSlotSource(index);
                slot.transform.localPosition = _prototypePosition + SlotOffset(index, descriptor);
                slot.transform.localRotation = SourceRotation(source);
                slot.transform.localScale = SourceScale(source);
            }

            ActiveSlotsChanged?.Invoke(_activeSlots);
        }

        private GameObject ResolveSlotSource(int slotIndex)
        {
            // source解決はArrangementの責務。cloneの生成・再利用・破棄はCacheへ委譲する。
            if (_sourceMode == ModelSourceMode.Same || _modelManager == null || _modelManager.SourceCount == 0)
                return _prototype;

            var sourceIndex = (_modelManager.CurrentSourceIndex + slotIndex) % _modelManager.SourceCount;
            return _modelManager.GetSource(sourceIndex);
        }

        private void PrewarmCurrentSource()
        {
            if (_prototype == null) return;

            _instanceCache.Prewarm(_prototype, MaximumSlotCount - 1);
            RefreshSlots();
        }

        private static Vector3 SlotOffset(int index, ArrangementDescriptor descriptor)
        {
            var x = index % descriptor.Counts.x;
            var y = index / descriptor.Counts.x % descriptor.Counts.y;
            var z = index / (descriptor.Counts.x * descriptor.Counts.y);

            return new Vector3(
                (x - (descriptor.Counts.x - 1) * 0.5f) * descriptor.Spacings.x,
                ((descriptor.Counts.y - 1) * 0.5f - y) * descriptor.Spacings.y,
                (z - (descriptor.Counts.z - 1) * 0.5f) * descriptor.Spacings.z);
        }

        private void MigrateArrangementSettings()
        {
            if (!_xyzArrangementMigrated)
            {
                var sideCount = Mathf.Clamp(_sideCount, 1, MaximumAxisCount);
                _counts = _mode switch
                {
                    ModelArrangementMode.Row => new Vector3Int(sideCount, 1, 1),
                    ModelArrangementMode.Column => new Vector3Int(1, sideCount, 1),
                    ModelArrangementMode.Grid => new Vector3Int(sideCount, sideCount, 1),
                    _ => Vector3Int.one
                };
                _spacings = Vector3.one * Mathf.Max(0f, _spacing);
                _xyzArrangementMigrated = true;
            }

            _counts = ClampCounts(_counts);
            _spacings = new Vector3(
                Mathf.Max(0f, _spacings.x),
                Mathf.Max(0f, _spacings.y),
                Mathf.Max(0f, _spacings.z));
        }

        private static Vector3Int ClampCounts(Vector3Int counts)
        {
            counts = new Vector3Int(
                Mathf.Clamp(counts.x, 1, MaximumAxisCount),
                Mathf.Clamp(counts.y, 1, MaximumAxisCount),
                Mathf.Clamp(counts.z, 1, MaximumAxisCount));
            while (counts.x * counts.y * counts.z > MaximumSlotCount)
            {
                if (counts.z > 1) counts.z--;
                else if (counts.y > 1) counts.y--;
                else counts.x--;
            }
            return counts;
        }

        private static ArrangementDescriptor CreateDescriptor(
            ModelArrangementMode mode,
            Vector3Int counts,
            Vector3 spacings)
        {
            var mappedCounts = mode switch
            {
                ModelArrangementMode.Row => new Vector3Int(counts.x, 1, 1),
                ModelArrangementMode.Column => new Vector3Int(1, counts.y, 1),
                ModelArrangementMode.Grid => counts,
                _ => Vector3Int.one
            };
            return new ArrangementDescriptor(mappedCounts, spacings);
        }

        private static int GetAxis(Vector3Int value, int axis)
        {
            return axis switch { 1 => value.y, 2 => value.z, _ => value.x };
        }

        private static float GetAxis(Vector3 value, int axis)
        {
            return axis switch { 1 => value.y, 2 => value.z, _ => value.x };
        }

        private static void SetAxis(ref Vector3Int value, int axis, int axisValue)
        {
            if (axis == 1) value.y = axisValue;
            else if (axis == 2) value.z = axisValue;
            else value.x = axisValue;
        }

        private static void SetAxis(ref Vector3 value, int axis, float axisValue)
        {
            if (axis == 1) value.y = axisValue;
            else if (axis == 2) value.z = axisValue;
            else value.x = axisValue;
        }

        private Vector3 SourcePosition(GameObject source)
        {
            return source != null && _sourcePositions.TryGetValue(source, out var position)
                ? position
                : source != null ? source.transform.localPosition : Vector3.zero;
        }

        private Vector3 SourceScale(GameObject source)
        {
            return source != null && _sourceScales.TryGetValue(source, out var scale)
                ? scale
                : source != null ? source.transform.localScale : Vector3.one;
        }

        private Quaternion SourceRotation(GameObject source)
        {
            return source != null && _sourceRotations.TryGetValue(source, out var rotation)
                ? rotation
                : source != null ? source.transform.localRotation : Quaternion.identity;
        }

        private void RestorePrototypeTransform()
        {
            if (_prototype != null)
            {
                _prototype.transform.localPosition = SourcePosition(_prototype);
                _prototype.transform.localScale = _prototypeScale;
                _prototype.transform.localRotation = _prototypeRotation;
            }
        }

        private readonly struct ArrangementDescriptor
        {
            public readonly Vector3Int Counts;
            public readonly Vector3 Spacings;
            public int Count => Counts.x * Counts.y * Counts.z;

            public ArrangementDescriptor(Vector3Int counts, Vector3 spacings)
            {
                Counts = counts;
                Spacings = spacings;
            }
        }

    }
}
