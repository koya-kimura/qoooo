using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using UnityEngine;

namespace qoooo.Rendering.Models
{
    public class ModelController : MonoBehaviour, IMidiParameterContainer
    {
        [SerializeField] private FloatParameter _scale = new("model.scale", "Model Scale", 5f, 40f, 20f);

        [SerializeField]
        private RadioParameter _motion = new("model.motion", "Model Motion", new[] { "Still", "Spin", "Pulse" });

        [SerializeField]
        private OneshotParameter _resetTransform = new("model.reset-transform", "Reset Model Transform");

        [SerializeField]
        private BoolParameter _active = new("model.active", "Model Active", true);

        private ModelManager _modelManager;
        private GameObject _currentModel;
        private Renderer[] _renderers = System.Array.Empty<Renderer>();

        private void Awake()
        {
            _modelManager = GetComponent<ModelManager>();
            if (_modelManager != null)
                _modelManager.CurrentModelChanged += SetCurrentModel;
        }

        private void Start()
        {
            SetCurrentModel(_modelManager?.CurrentModel);
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
            switch (_motion.Value)
            {
                case 1: _currentModel.transform.localRotation = Quaternion.Euler(0f, Time.time * 45f, 0f); break;
                case 2: scale *= 1f + 0.2f * Mathf.Sin(Time.time * 2f); break;
            }

            _currentModel.transform.localScale = Vector3.one * scale;
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
            SetRendererVisibility(_active.Value);
        }
    }
}
