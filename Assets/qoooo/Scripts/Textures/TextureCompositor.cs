using System;
using System.Collections.Generic;
using System.Linq;
using qoooo.View;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Textures
{
    [Serializable]
    public class ChromaKeyParams
    {
        public Color KeyColor = Color.green;
        [Min(0f)] public float Threshold = 0.01f;
        [Min(0.00001f)] public float Softness = 0.08f;
    }

    public class TextureCompositor : MonoBehaviour, ITextureContainer, IUiTarget
    {
        [SerializeField] private List<TextureLayer> _layers = new();
        [SerializeField] private Material _compositeMaterial;
        [SerializeField] private ChromaKeyParams _chromaKey = new();

        private readonly Dictionary<TextureLayer, RenderTexture> _keyedBuffers = new();
        private readonly List<RenderedLayer> _renderedLayers = new();
        private RenderTexture _bufferA;
        private RenderTexture _bufferB;
        private IResolutionContainer _resolutionContainer;
        private TextureLayer _selectedLayer;

        private static readonly Color SelectedLayerBackgroundColor = new(0.05f, 0.55f, 0.55f, 0.2f);

        private void Update()
        {
            CollectLayers();

            if (_renderedLayers.Count == 0 || _compositeMaterial == null)
            {
                Texture = null;
                return;
            }

            var resolution = _resolutionContainer?.Resolution
                             ?? new Vector2Int(_renderedLayers[0].Texture.width, _renderedLayers[0].Texture.height);
            EnsureBuffers(resolution.x, resolution.y);
            Clear(_bufferA);

            var read = _bufferA;
            var write = _bufferB;

            SetChromaKeyUniforms();
            foreach (var layer in _renderedLayers)
            {
                _compositeMaterial.SetTexture("_OverlayTex", layer.Texture);
                _compositeMaterial.SetInt("_UseChromaKey", layer.UseChromaKey ? 1 : 0);
                Graphics.Blit(read, write, _compositeMaterial, 0);
                (read, write) = (write, read);
            }

            Texture = read;
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
            ReleaseKeyedBuffers();
        }

        private void OnValidate()
        {
            _chromaKey ??= new ChromaKeyParams();
            _chromaKey.Threshold = Mathf.Max(0f, _chromaKey.Threshold);
            _chromaKey.Softness = Mathf.Max(0.00001f, _chromaKey.Softness);
        }

        public Texture Texture { get; private set; }
        public IReadOnlyList<TextureLayer> Layers => _layers;

        public Element CreateElement()
        {
            _chromaKey ??= new ChromaKeyParams();
            _layers ??= new List<TextureLayer>();
            _layers.RemoveAll(layer => layer == null);
            _selectedLayer ??= _layers.LastOrDefault();

            var listOption = new ListViewOption(
                reorderable: true,
                fixedSize: true,
                header: true,
                suppressAutoIndent: true)
            {
                createItemElementFunc = CreateLayerListItem
            };

            return UI.Column(
                UI.List("Layers (Bottom to Top)", () => _layers, listOption),
                UI.DynamicElementOnStatusChanged(
                    GetSelectedLayerId,
                    _ => CreateSelectedLayerElement()),
                UI.Fold("Global Chroma Key",
                    UI.Field("Color", () => _chromaKey.KeyColor),
                    UI.Field("Threshold", () => _chromaKey.Threshold),
                    UI.Field("Softness", () => _chromaKey.Softness)
                )
            );
        }

        private Element CreateLayerListItem(IBinder binder, int _)
        {
            TextureLayer GetLayer() => binder.GetObject() as TextureLayer;

            return UI.DynamicElementOnStatusChanged(
                () => GetLayer() == _selectedLayer,
                isSelected => UI.Row(
                        UI.Field(null,
                            () => GetLayer() != null && GetLayer().IsVisible,
                            visible => GetLayer()?.SetVisible(visible)),
                        UI.Button(UI.Label(
                                () => GetLayer() == null ? "Missing Layer" : GetLayer().DisplayName),
                            () => _selectedLayer = GetLayer())
                    )
                    .SetBackgroundColor(isSelected ? SelectedLayerBackgroundColor : null)
            );
        }

        private Element CreateSelectedLayerElement()
        {
            return _selectedLayer == null
                ? UI.HelpBox("Select a layer to edit its source and effects.")
                : UI.Column(
                    _selectedLayer.CreateParameterElement(),
                    UI.DynamicElementOnStatusChanged(
                        () => _selectedLayer.SourceComponentInstanceId,
                        _ => CreateSelectedSourceParameterElement())
                );
        }

        private Element CreateSelectedSourceParameterElement()
        {
            var parameterContainer = _selectedLayer?.SourceParameterContainer;
            return parameterContainer == null
                ? null
                : UI.Fold($"{_selectedLayer.SourceDisplayName} Parameters",
                    parameterContainer.CreateParameterElement()).Open();
        }

        private int GetSelectedLayerId()
        {
            return _selectedLayer != null ? _selectedLayer.GetInstanceID() : 0;
        }

        [Inject]
        public void Construct(IResolutionContainer resolutionContainer)
        {
            _resolutionContainer = resolutionContainer;
        }

        private void CollectLayers()
        {
            _renderedLayers.Clear();

            foreach (var layer in _layers)
            {
                if (layer == null || !layer.isActiveAndEnabled) continue;

                var source = layer.SourceTexture;
                if (source == null) continue;

                var preKey = layer.UseChromaKey && layer.HasEnabledEffect;
                if (preKey) source = ApplyChromaKey(layer, source);

                var texture = layer.ApplyEffect(source);
                if (texture != null)
                    _renderedLayers.Add(new RenderedLayer(
                        texture,
                        layer.UseChromaKey && !preKey));
            }
        }

        private Texture ApplyChromaKey(TextureLayer layer, Texture source)
        {
            if (!_keyedBuffers.TryGetValue(layer, out var target)
                || target == null
                || target.width != source.width
                || target.height != source.height)
            {
                ReleaseKeyedBuffer(layer);
                target = CreateBuffer(source.width, source.height, $"{layer.name} Chroma Key");
                _keyedBuffers[layer] = target;
            }

            SetChromaKeyUniforms();
            Graphics.Blit(source, target, _compositeMaterial, 1);
            return target;
        }

        private void SetChromaKeyUniforms()
        {
            _chromaKey ??= new ChromaKeyParams();
            _compositeMaterial.SetColor("_ChromaKeyColor", _chromaKey.KeyColor);
            _compositeMaterial.SetFloat("_ChromaThreshold", Mathf.Max(0f, _chromaKey.Threshold));
            _compositeMaterial.SetFloat("_ChromaSoftness", Mathf.Max(0.00001f, _chromaKey.Softness));
        }

        private void EnsureBuffers(int width, int height)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            if (_bufferA != null
                && _bufferB != null
                && _bufferA.width == width
                && _bufferA.height == height) return;

            ReleaseBuffers();
            _bufferA = CreateBuffer(width, height, "Composite A");
            _bufferB = CreateBuffer(width, height, "Composite B");
        }

        private RenderTexture CreateBuffer(int width, int height, string bufferName)
        {
            var buffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = bufferName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            buffer.Create();
            return buffer;
        }

        private void ReleaseBuffers()
        {
            ReleaseBuffer(ref _bufferA);
            ReleaseBuffer(ref _bufferB);
            Texture = null;
        }

        private void ReleaseKeyedBuffers()
        {
            foreach (var buffer in _keyedBuffers.Values)
            {
                if (buffer == null) continue;
                buffer.Release();
                Destroy(buffer);
            }

            _keyedBuffers.Clear();
        }

        private void ReleaseKeyedBuffer(TextureLayer layer)
        {
            if (!_keyedBuffers.Remove(layer, out var buffer) || buffer == null) return;

            buffer.Release();
            Destroy(buffer);
        }

        private void ReleaseBuffer(ref RenderTexture buffer)
        {
            if (buffer == null) return;

            buffer.Release();
            Destroy(buffer);
            buffer = null;
        }

        private static void Clear(RenderTexture target)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previous;
        }

        private readonly struct RenderedLayer
        {
            public readonly Texture Texture;
            public readonly bool UseChromaKey;

            public RenderedLayer(Texture texture, bool useChromaKey)
            {
                Texture = texture;
                UseChromaKey = useChromaKey;
            }
        }
    }
}
