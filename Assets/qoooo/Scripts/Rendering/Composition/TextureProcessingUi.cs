using System.Text;
using System.Linq;
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
            var parameters = source.EffectParameters;

            return UI.Column(
                UI.Fold("Active Pipeline",
                    UI.Label(() => $"1. UV       {ActiveEffectName(parameters.UvMode)}"),
                    UI.Label(() => $"2. Sampling {ActiveEffectName(parameters.SamplingMode)}"),
                    UI.Label(() => $"3. Color    {ActiveEffectName(parameters.ColorMode)}"),
                    UI.Label(() => $"4. Alpha    {ActiveEffectName(parameters.AlphaMode)}")
                ).Open(),
                UI.Fold("Effects",
                    UI.Dropdown("UV Effect", () => parameters.UvMode.Value,
                        value => parameters.UvMode.TrySetValue(value), parameters.UvMode.Options.ToArray()),
                    UI.Dropdown("Sampling Effect", () => parameters.SamplingMode.Value,
                        value => parameters.SamplingMode.TrySetValue(value), parameters.SamplingMode.Options.ToArray()),
                    UI.Dropdown("Color Effect", () => parameters.ColorMode.Value,
                        value => parameters.ColorMode.TrySetValue(value), parameters.ColorMode.Options.ToArray()),
                    UI.Dropdown("Alpha Effect", () => parameters.AlphaMode.Value,
                        value => parameters.AlphaMode.TrySetValue(value), parameters.AlphaMode.Options.ToArray()),
                    UI.Slider("Amount (px / angle)", () => parameters.Amount.Value,
                        value => parameters.Amount.TrySetValue(value), parameters.Amount.Min, parameters.Amount.Max),
                    UI.Slider("Scale / Frequency", () => parameters.Scale.Value,
                        value => parameters.Scale.TrySetValue(value), parameters.Scale.Min, parameters.Scale.Max),
                    UI.Slider("Threshold", () => parameters.Threshold.Value,
                        value => parameters.Threshold.TrySetValue(value), parameters.Threshold.Min, parameters.Threshold.Max),
                    UI.Slider("Strength", () => parameters.Strength.Value,
                        value => parameters.Strength.TrySetValue(value), parameters.Strength.Min, parameters.Strength.Max),
                    UI.Slider("Probability / Decay", () => parameters.Probability.Value,
                        value => parameters.Probability.TrySetValue(value), parameters.Probability.Min, parameters.Probability.Max),
                    UI.Slider("Seed", () => parameters.Seed.Value,
                        value => parameters.Seed.TrySetValue(value), parameters.Seed.Min, parameters.Seed.Max),
                    UI.Field("Direction / Phase", () => source.EffectParams.Direction, value => SetDirection(source, value)),
                    UI.Field("Grid", () => source.EffectParams.Grid, value => SetGrid(source, value)),
                    UI.Field("Tiling Scale", () => source.EffectParams.TileNum, value => SetTilingScale(source, value)),
                    UI.Field("Tiling Offset", () => source.EffectParams.Offset, value => SetTilingOffset(source, value)),
                    UI.Field("Palette Thresholds", () => source.EffectParams.Thresholds, value => SetThresholds(source, value)),
                    UI.Field("Color A", () => source.EffectParams.ColorA, value => SetColorA(source, value)),
                    UI.Field("Color B", () => source.EffectParams.ColorB, value => SetColorB(source, value)),
                    UI.Field("Color C", () => source.EffectParams.ColorC, value => SetColorC(source, value)),
                    UI.Field("Color D", () => source.EffectParams.ColorD, value => SetColorD(source, value))
                ).Open());
        }

        private static string ActiveEffectName(qoooo.Parameters.Model.RadioParameter parameter)
        {
            var value = parameter.Options[parameter.Value];
            if (value == "None") return "—";

            var label = new StringBuilder(value.Length + 4);
            for (var index = 0; index < value.Length; index++)
            {
                if (index > 0
                    && char.IsUpper(value[index])
                    && (char.IsLower(value[index - 1])
                        || index + 1 < value.Length && char.IsLower(value[index + 1])))
                    label.Append(' ');
                label.Append(value[index]);
            }
            return label.ToString();
        }

        private ProcessedTextureSource SelectedSource => _selectedTarget == 0 ? _modelSource : _backgroundSource;

        private void SetSelectedTarget(int target)
        {
            _selectedTarget = Mathf.Clamp(target, 0, TargetNames.Length - 1);
        }

        private static void SetDirection(ProcessedTextureSource source, Vector2 value)
        {
            var effectParams = source.EffectParams;
            effectParams.Direction = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetGrid(ProcessedTextureSource source, Vector2 value)
        {
            var effectParams = source.EffectParams;
            effectParams.Grid = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetThresholds(ProcessedTextureSource source, Vector3 value)
        {
            var effectParams = source.EffectParams;
            effectParams.Thresholds = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetColorA(ProcessedTextureSource source, Color value)
        {
            var effectParams = source.EffectParams;
            effectParams.ColorA = value;
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

        private static void SetColorB(ProcessedTextureSource source, Color value)
        {
            var effectParams = source.EffectParams;
            effectParams.ColorB = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetColorC(ProcessedTextureSource source, Color value)
        {
            var effectParams = source.EffectParams;
            effectParams.ColorC = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetColorD(ProcessedTextureSource source, Color value)
        {
            var effectParams = source.EffectParams;
            effectParams.ColorD = value;
            source.SetEffectParams(effectParams);
        }
    }
}