using System;
using System.Collections.Generic;
using System.Linq;
using qoooo.Presentation.Contracts;
using RosettaUI;
using UnityEngine;
using UnityEngine.Rendering;
using UnitySimpleContainer;

namespace qoooo.Rendering.Composition
{
    // Composites enabled layers bottom-to-top and owns the two ping-pong output buffers.
    // Its output feeds Preview and Syphon; source processing runs before this boundary.
    // SceneのModel → Poster → Previewというcamera順を前提に、Poster描画後の同一frameで合成する。
    public class TextureCompositor : MonoBehaviour, ITextureContainer, IUiTarget
    {
        [SerializeField] private List<TextureLayer> _layers = new();
        [SerializeField] private Material _compositeMaterial;
        [SerializeField] private Camera _posterCamera;

        private readonly List<RenderedLayer> _renderedLayers = new();
        private RenderTexture _bufferA;
        private RenderTexture _bufferB;
        private IResolutionContainer _resolutionContainer;
        private TextureLayer _selectedLayer;
        private int _compositedFrame = -1;

        private static readonly Color SelectedLayerBackgroundColor = new(0.05f, 0.55f, 0.55f, 0.2f);

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // UpdateではPosterCamera前のRTを読むため、Poster描画完了後だけを合成の起点にする。
            // frame guardは同一frameのcallbackによる二重Compositeを防ぐ。
            if (camera != _posterCamera || _compositedFrame == Time.frameCount) return;

            _compositedFrame = Time.frameCount;
            Composite();
        }

        private void Composite()
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

            foreach (var layer in _renderedLayers)
            {
                _compositeMaterial.SetTexture("_OverlayTex", layer.Texture);
                Graphics.Blit(read, write, _compositeMaterial, 0);
                (read, write) = (write, read);
            }

            Texture = read;
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }

        public Texture Texture { get; private set; }
        public IReadOnlyList<TextureLayer> Layers => _layers;

        public Element CreateElement()
        {
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
                    _ => CreateSelectedLayerElement())
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
                ? UI.HelpBox("Select a layer to inspect its output source.")
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

                var texture = layer.ApplyEffect(layer.SourceTexture);
                if (texture != null)
                    _renderedLayers.Add(new RenderedLayer(texture));
            }
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

            public RenderedLayer(Texture texture)
            {
                Texture = texture;
            }
        }
    }
}
