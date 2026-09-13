using qoooo.Textures;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Controller
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour, ITextureContainer
    {
        private Camera _camera;
        private RenderTexture _output;
        private IResolutionContainer _resolutionContainer;

        private Vector2Int ValidResolution =>
            _resolutionContainer?.Resolution ?? new Vector2Int(1920, 1080);

        public Texture Texture => _output;

        [Inject]
        public void Construct(IResolutionContainer resolutionContainer)
        {
            _resolutionContainer = resolutionContainer;
        }

        private void Start()
        {
            _camera = GetComponent<Camera>();
            EnsureTexture();
        }

        private void Update()
        {
            EnsureTexture();
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }

        private void EnsureTexture()
        {
            var resolution = ValidResolution;
            if (_output != null
                && _output.width == resolution.x
                && _output.height == resolution.y) return;

            ReleaseTexture();
            _output = new RenderTexture(resolution.x, resolution.y, 24)
            {
                name = $"{name} Texture"
            };
            _output.Create();
            _camera.targetTexture = _output;
        }

        private void ReleaseTexture()
        {
            if (_output == null) return;
            if (_camera != null && _camera.targetTexture == _output) _camera.targetTexture = null;
            _output.Release();
            Destroy(_output);
            _output = null;
        }
    }
}
