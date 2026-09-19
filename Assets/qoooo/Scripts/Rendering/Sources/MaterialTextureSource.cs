using RosettaUI;
using qoooo.Foundation.Timing;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Rendering.Composition;
using UnityEngine;
using UnityEngine.Serialization;
using UnitySimpleContainer;

namespace qoooo.Rendering.Sources
{
    public enum ProceduralPatternType
    {
        NoiseTexture = 0,
        DiagonalStripe = 1,
        VerticalStripe = 2,
        HorizontalStripe = 3,
        WaveStripe = 4,
        Checkerboard = 5,
        PolkaDot = 6,
        Sunburst = 7,
        GridLine = 8,
        PsychedelicRing = 9
    }

    [DefaultExecutionOrder(-100)]
    public class MaterialTextureSource : MonoBehaviour, ITextureContainer, IParameterContainer, IMidiParameterSource
    {
        [SerializeField] private Material _material;
        [SerializeField]
        private RadioParameter _pattern = new(
            "background.pattern", "Background Pattern",
            new[]
            {
                "Noise Texture", "Diagonal Stripe", "Vertical Stripe", "Horizontal Stripe", "Wave Stripe",
                "Checkerboard", "Polka Dot", "Sunburst", "Grid Line", "Psychedelic Ring"
            },
            (int)ProceduralPatternType.NoiseTexture);
        [SerializeField, FormerlySerializedAs("_colorA")]
        private Color _mainColor = new(0.025f, 0.025f, 0.04f, 1f);
        [SerializeField, FormerlySerializedAs("_colorB")]
        private Color _subColor = new(0.12f, 0.12f, 0.18f, 1f);
        [SerializeField] private Vector2 _tiling = new(12f, 8f);
        [SerializeField] private Vector2 _offset;
        [SerializeField] private Vector2 _speed = new(0.15f, 0f);
        [SerializeField, Range(0f, 1f)] private float _opacity = 1f;

        private RenderTexture _output;
        private IResolutionContainer _resolutionContainer;
        private IBpmSource _bpmSource;
        private bool _materialContractValidated;
        private bool _hasValidMaterialContract;

        public Texture Texture => _output;

        public System.Collections.Generic.IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get { yield return _pattern; }
        }

        [Inject]
        public void Construct(IResolutionContainer resolutionContainer, IBpmSource bpmSource)
        {
            _resolutionContainer = resolutionContainer;
            _bpmSource = bpmSource;
        }

        private void Update()
        {
            EnsureTexture();
            if (_material == null) return;

            if (!SetUniforms()) return;
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

        private bool SetUniforms()
        {
            if (!ValidateMaterialContract()) return false;
            _material.SetInt("_PatternType", _pattern.Value);
            _material.SetColor("_MainColor", _mainColor);
            _material.SetColor("_SubColor", _subColor);
            _material.SetVector("_Tiling", _tiling);
            _material.SetVector("_Offset", _offset);
            _material.SetVector("_Speed", _speed);
            _material.SetFloat("_Opacity", _opacity);
            _material.SetFloat("_Beat", (float)(_bpmSource?.Beat ?? 0d));
            return true;
        }

        public Element CreateParameterElement()
        {
            return UI.Column(
                UI.Fold("Pattern", CreatePatternRadioElement()).Open(),
                UI.Field("Main Color", () => _mainColor),
                UI.Field("Sub Color", () => _subColor),
                UI.Field("Tiling", () => _tiling),
                UI.Field("Offset", () => _offset),
                UI.Field("Speed", () => _speed),
                UI.Field("Opacity", () => _opacity)
            );
        }

        private Element CreatePatternRadioElement()
        {
            return UI.DynamicElementOnStatusChanged(
                () => _pattern.Value,
                selected =>
                {
                    var buttons = new System.Collections.Generic.List<Element>();
                    for (var index = 0; index < _pattern.Options.Count; index++)
                    {
                        var optionIndex = index;
                        var isSelected = optionIndex == selected;
                        buttons.Add(UI.Button(
                                $"{(isSelected ? "●" : "○")} {_pattern.Options[optionIndex]}",
                                () => _pattern.TrySetValue(optionIndex))
                            .SetBackgroundColor(isSelected ? new Color(0.08f, 0.45f, 0.55f, 0.45f) : null));
                    }

                    return UI.Column(buttons);
                });
        }

        private void OnValidate()
        {
            _opacity = Mathf.Clamp01(_opacity);
            _pattern ??= new RadioParameter(
                "background.pattern", "Background Pattern",
                new[]
                {
                    "Noise Texture", "Diagonal Stripe", "Vertical Stripe", "Horizontal Stripe", "Wave Stripe",
                    "Checkerboard", "Polka Dot", "Sunburst", "Grid Line", "Psychedelic Ring"
                });
            _materialContractValidated = false;
            if (_material != null) SetUniforms();
        }

        private bool ValidateMaterialContract()
        {
            if (_materialContractValidated) return _hasValidMaterialContract;

            _materialContractValidated = true;
            _hasValidMaterialContract = _material != null;
            var requiredProperties = new[]
            {
                "_PatternType", "_MainColor", "_SubColor", "_Tiling", "_Offset", "_Speed", "_Opacity", "_Beat"
            };

            foreach (var propertyName in requiredProperties)
            {
                if (_material != null && _material.HasProperty(propertyName)) continue;
                _hasValidMaterialContract = false;
                Debug.LogError($"[Background] Material '{_material?.name ?? "<none>"}' is missing required property '{propertyName}'.", this);
            }

            return _hasValidMaterialContract;
        }
    }
}
