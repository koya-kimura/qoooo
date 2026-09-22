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

    // OWNER / Arrangement state: 選択中sourceを同一モデルのslot群として管理する。
    // WHY / Poster上のTexture repeatではなく、Model Cameraに映る3D object配置を扱う。
    public sealed class ModelArrangementController : MonoBehaviour, IUiTarget
    {
        [SerializeField] private ModelArrangementMode _mode;
        [SerializeField, Range(1, 9)] private int _count = 1;
        [SerializeField, Range(1, 3)] private int _columns = 3;
        [SerializeField, Min(0f)] private float _spacing = 2f;

        private readonly List<GameObject> _clones = new();
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
                UI.Field("Count", () => _count, SetCount),
                UI.Field("Columns", () => _columns, SetColumns),
                UI.Field("Spacing", () => _spacing, SetSpacing))
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
            ClearClones();
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

        private void SetCount(int value)
        {
            _count = Mathf.Clamp(value, 1, 9);
            RefreshSlots();
        }

        private void SetColumns(int value)
        {
            _columns = Mathf.Clamp(value, 1, 3);
            RefreshSlots();
        }

        private void SetSpacing(float value)
        {
            _spacing = Mathf.Max(0f, value);
            RefreshSlots();
        }

        // FLOW / source -> runtime slots -> 1 Model Camera。clone生成はcount増加時だけ行う。
        private void RefreshSlots()
        {
            if (_prototype == null)
            {
                _activeSlots.Clear();
                ActiveSlotsChanged?.Invoke(_activeSlots);
                return;
            }

            var requiredClones = Mathf.Max(0, _count - 1);
            while (_clones.Count < requiredClones)
            {
                var clone = Instantiate(_prototype, transform);
                clone.name = $"{_prototype.name} Arrangement Slot";
                clone.transform.localScale = _prototypeScale;
                clone.transform.localRotation = _prototypeRotation;
                clone.SetActive(true);
                _clones.Add(clone);
            }

            _activeSlots.Clear();
            _prototype.SetActive(true);
            _activeSlots.Add(_prototype);
            for (var index = 0; index < _clones.Count; index++)
            {
                var clone = _clones[index];
                if (clone == null) continue;

                var enabled = index < requiredClones;
                clone.SetActive(enabled);
                if (enabled) _activeSlots.Add(clone);
            }

            for (var index = 0; index < _activeSlots.Count; index++)
            {
                var slot = _activeSlots[index];
                if (slot == null) continue;

                slot.transform.localPosition = _prototypePosition + SlotOffset(index);
            }

            ActiveSlotsChanged?.Invoke(_activeSlots);
        }

        private Vector3 SlotOffset(int index)
        {
            var columns = _mode switch
            {
                ModelArrangementMode.Single => 1,
                ModelArrangementMode.Row => _count,
                ModelArrangementMode.Column => 1,
                _ => Mathf.Min(_columns, _count)
            };
            var rows = _mode == ModelArrangementMode.Column
                ? _count
                : Mathf.CeilToInt(_count / (float)columns);
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
                if (clone == null) continue;
                clone.SetActive(false);
                Destroy(clone);
            }

            _clones.Clear();
            _activeSlots.Clear();
        }
    }
}