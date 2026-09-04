using qoooo.Interfaces;
using UnityEngine;

namespace qoooo.Controller
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour, ITextureContainer
    {
        [SerializeField] private Vector2Int _resolution;
        [SerializeField] private TextureUsage _usage;

        private Camera _camera;
        private RenderTexture _output;

        public Texture Texture => _output;
        public TextureUsage Usage => _usage;

        private Vector2Int ValidResolution => new (
            Mathf.Max(1, _resolution.x),
            Mathf.Max(1, _resolution.y)
        );

        private void Start()
        {
            ReleaseTexture();
            _output = new RenderTexture(ValidResolution.x, ValidResolution.y, 0);
            _output.Create();

            _camera = GetComponent<Camera>();
            _camera.targetTexture = _output;
        }

        private void ReleaseTexture()
        {
            if (_output == null) return;
            if (_camera != null && _camera.targetTexture == _output)
            {
                _camera.targetTexture = null;
            }
            _output.Release();
            Destroy(_output);
            _output = null;
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }
    }
}
