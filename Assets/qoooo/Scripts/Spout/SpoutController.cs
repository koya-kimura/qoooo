using Klak.Spout;
using PrefsGUI;
using PrefsGUI.RosettaUI;
using qoooo.View;
using RosettaUI;
using UnityEngine;

namespace qoooo.Spout
{
    public class SpoutController : MonoBehaviour, IUiTarget
    {
        [SerializeField] Camera _camera;
        [SerializeField] SpoutSender _spoutSender;

        private PrefsVector2Int _resolution = new ("resolution", new Vector2Int(1920, 1080));
        private PrefsString _spoutName = new ("spoutName", "qoooo");

        private RenderTexture _output;

        private void Start()
        {
            ApplySpoutName();
            RecreateTexture();
        }

        private void ApplySpoutName()
        {
            var spoutName = _spoutName.Get().Trim();
            if (string.IsNullOrEmpty(spoutName)) return;

            _spoutSender.spoutName = spoutName;
        }

        private void RecreateTexture()
        {
            var resolution = _resolution.Get();
            var validResolution = new Vector2Int(
                Mathf.Max(1, resolution.x),
                Mathf.Max(1, resolution.y)
                );

            if (resolution != validResolution)
            {
                _resolution.Set(validResolution);
                return;
            }

            if (_output != null &&
                _output.width == validResolution.x &&
                _output.height == validResolution.y)
            {
                return;
            }

            ReleaseTexture();

            _output = new RenderTexture(validResolution.x, validResolution.y, 0);
            _output.Create();

            _camera.targetTexture = _output;
            _spoutSender.sourceTexture = _output;
        }

        private void ReleaseTexture()
        {
            if (_output == null) return;

            if (_camera != null && _camera.targetTexture == _output)
            {
                _camera.targetTexture = null;
            }

            if (_spoutSender != null && _spoutSender.sourceTexture == _output)
            {
                _spoutSender.sourceTexture = null;
            }

            _output.Release();
            Destroy(_output);
            _output = null;
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }

        public Element CreateElement()
        {
            return UI.Column(
                _resolution.CreateElement(UI.Label("Resolution")),
                _spoutName.CreateElement(UI.Label("Spout Name")),
                UI.Button("Apply", () => { ApplySpoutName(); RecreateTexture(); })
            );
        }
    }
}
