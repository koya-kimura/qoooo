using qoooo.Presentation.Contracts;
using RosettaUI;
using UnityEngine;

namespace qoooo.Rendering.Composition
{
    // UIはstateを持たず、選択中のsourceが所有するTextureEffectParamsを直接編集する。
    public sealed class TextureProcessingUi : MonoBehaviour, IUiTarget
    {
        private static readonly string[] TargetNames = { "Model", "Background" };

        [SerializeField] private ProcessedTextureSource _modelSource;
        [SerializeField] private ProcessedTextureSource _backgroundSource;

        // TargetはTexture処理の値ではなく、このWindow内だけのnavigation state。
        private int _selectedTarget;

        public Element CreateElement()
        {
            return UI.Column(
                UI.Dropdown("Target", () => _selectedTarget, SetSelectedTarget, TargetNames),
                UI.DynamicElementOnStatusChanged(
                    () => _selectedTarget,
                    _ => CreateEffectsElement()));
        }

        private Element CreateEffectsElement()
        {
            var source = SelectedSource;
            if (source == null) return UI.HelpBox("Selected texture source is not assigned.");

            return UI.Fold("Effects",
                UI.Field("Invert", () => source.EffectParams.IsInvert, value => SetInvert(source, value)),
                UI.Field("Mosaic", () => source.EffectParams.IsMosaic, value => SetMosaic(source, value)),
                UI.Field("Mosaic Size", () => source.EffectParams.MosaicSize, value => SetMosaicSize(source, value)),
                UI.Field("Tiling", () => source.EffectParams.IsTiling, value => SetTiling(source, value)),
                UI.Field("Tiling Scale", () => source.EffectParams.TileNum, value => SetTilingScale(source, value)),
                UI.Field("Tiling Offset", () => source.EffectParams.Offset, value => SetTilingOffset(source, value)),
                UI.Field("Posterize", () => source.EffectParams.IsPosterize, value => SetPosterize(source, value)),
                UI.Field("Posterize Levels", () => source.EffectParams.PosterizeLevels, value => SetPosterizeLevels(source, value))
            ).Open();
        }

        private ProcessedTextureSource SelectedSource => _selectedTarget == 0 ? _modelSource : _backgroundSource;

        private void SetSelectedTarget(int target)
        {
            _selectedTarget = Mathf.Clamp(target, 0, TargetNames.Length - 1);
        }

        private static void SetInvert(ProcessedTextureSource source, bool value)
        {
            var effectParams = source.EffectParams;
            effectParams.IsInvert = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetMosaic(ProcessedTextureSource source, bool value)
        {
            var effectParams = source.EffectParams;
            effectParams.IsMosaic = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetMosaicSize(ProcessedTextureSource source, float value)
        {
            var effectParams = source.EffectParams;
            effectParams.MosaicSize = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetTiling(ProcessedTextureSource source, bool value)
        {
            var effectParams = source.EffectParams;
            effectParams.IsTiling = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetTilingScale(ProcessedTextureSource source, Vector2 value)
        {
            var effectParams = source.EffectParams;
            effectParams.TileNum = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetTilingOffset(ProcessedTextureSource source, Vector2 value)
        {
            var effectParams = source.EffectParams;
            effectParams.Offset = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetPosterize(ProcessedTextureSource source, bool value)
        {
            var effectParams = source.EffectParams;
            effectParams.IsPosterize = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetPosterizeLevels(ProcessedTextureSource source, float value)
        {
            var effectParams = source.EffectParams;
            effectParams.PosterizeLevels = value;
            source.SetEffectParams(effectParams);
        }
    }
}