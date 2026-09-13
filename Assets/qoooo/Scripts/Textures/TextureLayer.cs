using RosettaUI;
using UnityEngine;

namespace qoooo.Textures
{
    public class TextureLayer : MonoBehaviour, IParameterContainer
    {
        [SerializeField] private MonoBehaviour _sourceComponent;
        [SerializeField] private TextureEffect _effect;
        [SerializeField] private bool _useChromaKey = true;
        [SerializeField] private EffectParams _effectParams;

        public string DisplayName => gameObject.name;
        public bool IsVisible => gameObject.activeSelf;
        public bool UseChromaKey => _useChromaKey;

        public bool HasEnabledEffect => _effect != null
                                        && _effectParams.HasEnabledEffect;

        public Texture SourceTexture => (_sourceComponent as ITextureContainer)?.Texture;
        public IParameterContainer SourceParameterContainer => _sourceComponent as IParameterContainer;
        public int SourceComponentInstanceId => _sourceComponent != null ? _sourceComponent.GetInstanceID() : 0;
        public string SourceDisplayName => _sourceComponent != null ? _sourceComponent.gameObject.name : "Source";

        public void SetDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return;
            gameObject.name = displayName.Trim();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetSourceComponent(MonoBehaviour sourceComponent)
        {
            if (sourceComponent != null && sourceComponent is not ITextureContainer)
            {
                Debug.LogWarning("Source Component must implement ITextureContainer.", sourceComponent);
                return;
            }

            _sourceComponent = sourceComponent;
        }

        private void OnValidate()
        {
            _effectParams.MosaicSize = Mathf.Max(1f, _effectParams.MosaicSize);

            if (_sourceComponent != null && _sourceComponent is not ITextureContainer)
                Debug.LogError("Source Component must implement ITextureContainer.", this);
        }

        public Element CreateParameterElement()
        {
            return UI.Column(
                UI.Fold("Layer",
                    UI.Field("Name", () => DisplayName, SetDisplayName),
                    UI.Field("Visible", () => IsVisible, SetVisible),
                    UI.Field("Source", () => _sourceComponent, SetSourceComponent),
                    UI.Field("Use Chroma Key", () => _useChromaKey)
                ).Open(),
                UI.Fold("Effects",
                    UI.Field("Invert", () => _effectParams.IsInvert),
                    UI.Field("Mosaic", () => _effectParams.IsMosaic),
                    UI.Field("Mosaic Size", () => _effectParams.MosaicSize),
                    UI.Field("Tiling", () => _effectParams.IsTiling),
                    UI.Field("Tiling Offset", () => _effectParams.Offset)
                ).Open()
            );
        }

        public Texture Render()
        {
            return ApplyEffect(SourceTexture);
        }

        public Texture ApplyEffect(Texture source)
        {
            var current = source;
            if (current == null) return null;
            if (_effect != null) current = _effect.ApplyTexture(current, _effectParams);

            return current;
        }
    }
}
