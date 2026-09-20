using qoooo.Rendering.Composition;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Rendering.Sources
{
    public class PosterCanvasSource : MonoBehaviour, ITextureContainer
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Canvas _canvas;

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

        private void OnEnable()
        {
            ConfigureCanvas();
            EnsureTexture();
        }

        private void Update()
        {
            EnsureTexture();
        }

        private void OnDisable()
        {
            ReleaseTexture();
        }

        private void OnDestroy()
        {
            ReleaseTexture();
        }

        private void OnValidate()
        {
            ConfigureCanvas();
        }

        private void ConfigureCanvas()
        {
            if (_canvas == null || _camera == null) return;

            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _camera;
        }

        private void EnsureTexture()
        {
            if (_camera == null) return;

            var resolution = ValidResolution;
            if (_output != null
                && _output.width == resolution.x
                && _output.height == resolution.y) return;

            ReleaseTexture();
            _output = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32)
            {
                name = $"{name} Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
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