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

        private readonly Quaternion _initialRotation = Quaternion.identity;
        private Renderer[] _renderers;

        private void Awake()
        {
            var modelRoot = transform.parent != null ? transform.parent : transform;
            _renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            if (_resetTransform.Value) transform.localRotation = _initialRotation;
            var scale = _scale.Value;
            switch (_motion.Value)
            {
                case 1: transform.localRotation = Quaternion.Euler(0f, Time.time * 45f, 0f); break;
                case 2: scale *= 1f + 0.2f * Mathf.Sin(Time.time * 2f); break;
            }

            transform.localScale = Vector3.one * scale;
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
    }
}
