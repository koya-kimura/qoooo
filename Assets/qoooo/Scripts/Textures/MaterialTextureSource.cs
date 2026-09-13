using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Textures
{
    public enum ProceduralPatternType
    {
        Checker = 0,
        Stripes = 1,
        Solid = 2
    }

    [DefaultExecutionOrder(-100)]
    public class MaterialTextureSource : MonoBehaviour, ITextureContainer, IParameterContainer
    {
        [SerializeField] private Material _material;
        [SerializeField] private ProceduralPatternType _patternType = ProceduralPatternType.Checker;
        [SerializeField] private Color _colorA = new(0.025f, 0.025f, 0.04f, 1f);
        [SerializeField] private Color _colorB = new(0.12f, 0.12f, 0.18f, 1f);
        [SerializeField] private Vector2 _tiling = new(12f, 8f);
        [SerializeField] private Vector2 _offset;
        [SerializeField] private Vector2 _speed = new(0.15f, 0f);
        [SerializeField, Range(0f, 1f)] private float _opacity = 1f;

        private RenderTexture _output;
        private IResolutionContainer _resolutionContainer;

        public Texture Texture => _output;

        [Inject]
        public void Construct(IResolutionContainer resolutionContainer)
        {
            _resolutionContainer = resolutionContainer;
        }

        private void Update()
        {
            EnsureTexture();
            if (_material == null) return;

            SetUniforms();
            Graphics.Blit(Texture2D.blackTexture, _output, _material);
        }

        private void OnDisable()
        {
            ReleaseTexture();
        }

        private void EnsureTexture()
        {
            var resolution = _resolutionContainer?.Resolution ?? new Vector2Int(1920, 1080);
            var width = Mathf.Max(1, resolution.x);
            var height = Mathf.Max(1, resolution.y);
            if (_output != null && _output.width == width && _output.height == height) return;

            ReleaseTexture();
            _output = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = $"{name} Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            _output.Create();
        }

        private void ReleaseTexture()
        {
            if (_output == null) return;

            _output.Release();
            Destroy(_output);
            _output = null;
        }

        private void SetUniforms()
        {
            _material.SetInt("_PatternType", (int)_patternType);
            _material.SetInt("_PatternChecker", (int)ProceduralPatternType.Checker);
            _material.SetInt("_PatternStripes", (int)ProceduralPatternType.Stripes);
            _material.SetInt("_PatternSolid", (int)ProceduralPatternType.Solid);
            _material.SetColor("_ColorA", _colorA);
            _material.SetColor("_ColorB", _colorB);
            _material.SetVector("_Tiling", _tiling);
            _material.SetVector("_Offset", _offset);
            _material.SetVector("_Speed", _speed);
            _material.SetFloat("_Opacity", _opacity);
        }

        public Element CreateParameterElement()
        {
            return UI.Column(
                UI.Field("Pattern", () => _patternType),
                UI.Field("Color A", () => _colorA),
                UI.Field("Color B", () => _colorB),
                UI.Field("Tiling", () => _tiling),
                UI.Field("Offset", () => _offset),
                UI.Field("Speed", () => _speed),
                UI.Field("Opacity", () => _opacity)
            );
        }

        private void OnValidate()
        {
            _opacity = Mathf.Clamp01(_opacity);
            if (_material != null) SetUniforms();
        }
    }
}
