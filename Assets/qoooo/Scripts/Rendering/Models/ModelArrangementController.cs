using System;
using System.Collections.Generic;
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
    public sealed class ModelArrangementController : MonoBehaviour, IUiTarget
    {
        private const int MaximumSideCount = 4;
        private const int MaximumSlotCount = MaximumSideCount * MaximumSideCount;

        [SerializeField] private ModelArrangementMode _mode;
        [SerializeField, Range(1, MaximumSideCount)] private int _sideCount = 1;
        [SerializeField, Min(0f)] private float _spacing = 0.25f;
        [SerializeField] private ModelSourceMode _sourceMode;

        private readonly List<CloneSlot> _clones = new();
        private readonly List<GameObject> _activeSlots = new();
        private readonly Dictionary<GameObject, Vector3> _sourcePositions = new();
        private readonly Dictionary<GameObject, Vector3> _sourceScales = new();
        private readonly Dictionary<GameObject, Quaternion> _sourceRotations = new();
        private ModelManager _modelManager;
        private GameObject _prototype;
        private Vector3 _prototypePosition;
        private Vector3 _prototypeScale;
        private Quaternion _prototypeRotation;

        public IReadOnlyList<GameObject> ActiveSlots => _activeSlots;
        public event Action<IReadOnlyList<GameObject>> ActiveSlotsChanged;

        private void Awake()
        {
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
            SetSource(_modelManager?.CurrentModel);
            PrewarmCurrentSource();
        }

        private void OnDestroy()
        {
            if (_modelManager != null)
                _modelManager.CurrentModelChanged -= SetSource;

            RestorePrototypeTransform();
            ClearClones();
        }

        public Element CreateElement()
        {
            return UI.Fold("Arrangement",
                UI.Dropdown("Mode", () => (int)_mode, SetMode, Enum.GetNames(typeof(ModelArrangementMode))),
                UI.Field("Side Count", () => _sideCount, SetSideCount),
                UI.Field("Spacing", () => _spacing, SetSpacing),
                UI.Dropdown("Source Mode", () => (int)_sourceMode, SetSourceMode,
                    Enum.GetNames(typeof(ModelSourceMode))))
                .Open();
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

        private void SetMode(int value)
        {
            _mode = (ModelArrangementMode)Mathf.Clamp(value, 0, Enum.GetValues(typeof(ModelArrangementMode)).Length - 1);
            RefreshSlots();
        }

        private void SetSideCount(int value)
        {
            _sideCount = Mathf.Clamp(value, 1, MaximumSideCount);
            RefreshSlots();
        }

        private void SetSpacing(float value)
        {
            _spacing = Mathf.Max(0f, value);
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
            _prototype.SetActive(true);
            _activeSlots.Add(_prototype);
            var slotCount = SlotCount;
            for (var index = 1; index < slotCount; index++)
            {
                var slot = GetOrCreateClone(index - 1, ResolveSlotSource(index));
                if (slot?.Instance == null) continue;

                slot.Instance.SetActive(true);
                _activeSlots.Add(slot.Instance);
            }

            for (var index = slotCount - 1; index < _clones.Count; index++)
            {
                var slot = _clones[index];
                if (slot?.Instance != null) slot.Instance.SetActive(false);
            }

            for (var index = 0; index < _activeSlots.Count; index++)
            {
                var slot = _activeSlots[index];
                if (slot == null) continue;

                slot.transform.localPosition = _prototypePosition + SlotOffset(index);
            }

            ActiveSlotsChanged?.Invoke(_activeSlots);
        }

        private GameObject ResolveSlotSource(int slotIndex)
        {
            if (_sourceMode == ModelSourceMode.Same || _modelManager == null || _modelManager.SourceCount == 0)
                return _prototype;

            var sourceIndex = (_modelManager.CurrentSourceIndex + slotIndex) % _modelManager.SourceCount;
            return _modelManager.GetSource(sourceIndex);
        }

        private int SlotCount => _mode switch
        {
            ModelArrangementMode.Single => 1,
            ModelArrangementMode.Grid => _sideCount * _sideCount,
            _ => _sideCount
        };

        private CloneSlot GetOrCreateClone(int cloneIndex, GameObject source)
        {
            while (_clones.Count <= cloneIndex) _clones.Add(new CloneSlot());

            var slot = _clones[cloneIndex];
            if (slot.Source == source && slot.Instance != null) return slot;

            if (slot.Instance != null)
            {
                slot.Instance.SetActive(false);
                Destroy(slot.Instance);
            }

            slot.Source = source;
            slot.Instance = source == null ? null : Instantiate(source, transform);
            if (slot.Instance != null)
            {
                slot.Instance.name = $"{source.name} Arrangement Slot";
                slot.Instance.transform.localScale = SourceScale(source);
                slot.Instance.transform.localRotation = SourceRotation(source);
                slot.Instance.SetActive(true);
            }

            return slot;
        }

        private void PrewarmCurrentSource()
        {
            if (_prototype == null) return;

            for (var index = 0; index < MaximumSlotCount - 1; index++)
            {
                var slot = GetOrCreateClone(index, _prototype);
                if (slot?.Instance != null) slot.Instance.SetActive(false);
            }

            RefreshSlots();
        }

        private Vector3 SlotOffset(int index)
        {
            var columns = _mode switch
            {
                ModelArrangementMode.Single => 1,
                ModelArrangementMode.Row => _sideCount,
                ModelArrangementMode.Column => 1,
                _ => _sideCount
            };
            var rows = _mode == ModelArrangementMode.Column
                ? _sideCount
                : _mode == ModelArrangementMode.Grid ? _sideCount : 1;
            var column = _mode == ModelArrangementMode.Column ? 0 : index % columns;
            var row = _mode == ModelArrangementMode.Row ? 0 : index / columns;

            return new Vector3(
                (column - (columns - 1) * 0.5f) * _spacing,
                ((rows - 1) * 0.5f - row) * _spacing,
                0f);
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

        private void ClearClones()
        {
            foreach (var clone in _clones)
            {
                if (clone?.Instance == null) continue;
                clone.Instance.SetActive(false);
                Destroy(clone.Instance);
            }

            _clones.Clear();
            _activeSlots.Clear();
        }

        private sealed class CloneSlot
        {
            public GameObject Source;
            public GameObject Instance;
        }
    }
}