using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Presentation.Contracts;
using RosettaUI;
using UnityEngine;

namespace qoooo.Rendering.Composition
{
    // Effect values remain on their source; Edit opens an independent inspector window for that source and effect.
    public sealed class TextureProcessingUi : MonoBehaviour, IUiTarget
    {
        private static readonly string[] TargetNames = { "Model", "Background", "Output" };

        [SerializeField] private ProcessedTextureSource _modelSource;
        [SerializeField] private ProcessedTextureSource _backgroundSource;
        [SerializeField] private ProcessedTextureSource _outputSource;

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
                    UI.Label(() => $"1. UV        {ActiveEffectNames(TextureEffectStage.Uv, parameters)}"),
                    UI.Label(() => $"2. Sampling  {ActiveEffectNames(TextureEffectStage.Sampling, parameters)}"),
                    UI.Label(() => $"3. Color     {ActiveEffectNames(TextureEffectStage.Color, parameters)}"),
                    UI.Label(() => $"4. Alpha     {ActiveEffectNames(TextureEffectStage.Alpha, parameters)}")
                ).Open(),
                UI.Fold("Effects",
                    CreateStageRack("UV", TextureEffectStage.Uv, source),
                    CreateStageRack("Sampling", TextureEffectStage.Sampling, source),
                    CreateStageRack("Color", TextureEffectStage.Color, source),
                    CreateStageRack("Alpha", TextureEffectStage.Alpha, source)
                ).Open());
        }

        private Element CreateStageRack(string label, TextureEffectStage stage, ProcessedTextureSource source)
        {
            var rows = new List<Element>();
            var targetName = TargetNames[_selectedTarget];
            foreach (var definition in TextureEffectUiMetadata.ForStage(stage))
            {
                var selected = definition;
                rows.Add(UI.Row(
                    UI.Field(null,
                        () => source.EffectParameters.IsEffectEnabled(stage, selected.Value),
                        active => source.EffectParameters.SetEffectEnabled(stage, selected.Value, active)),
                    UI.Label(selected.Label),
                    UI.Button("Edit", () => OpenInspector(targetName, source, selected))));
            }

            return UI.Fold(label, UI.Column(rows));
        }

        private static void OpenInspector(
            string targetName,
            ProcessedTextureSource source,
            TextureEffectUiDefinition definition)
        {
            var controls = CreateInspectorControls(source, definition.Parameters);
            controls.Insert(0, UI.Label($"Target: {targetName}"));
            controls.Insert(1, UI.Label($"Stage: {StageLabel(definition.Stage)}"));
            controls.Insert(2, UI.Label($"Effect: {definition.Label}"));
            UI.Window($"Effect Inspector - {definition.Label}", UI.Column(controls)).Open();
        }

        private static List<Element> CreateInspectorControls(
            ProcessedTextureSource source,
            TextureEffectUiParameter visible)
        {
            var parameters = source.EffectParameters;
            var controls = new List<Element>();

            if (visible.HasFlag(TextureEffectUiParameter.Amount))
                controls.Add(FloatSlider("Amount (px / angle)", parameters.Amount));
            if (visible.HasFlag(TextureEffectUiParameter.Scale))
                controls.Add(FloatSlider("Scale / Frequency", parameters.Scale));
            if (visible.HasFlag(TextureEffectUiParameter.Threshold))
                controls.Add(FloatSlider("Threshold", parameters.Threshold));
            if (visible.HasFlag(TextureEffectUiParameter.Strength))
                controls.Add(FloatSlider("Strength", parameters.Strength));
            if (visible.HasFlag(TextureEffectUiParameter.Probability))
                controls.Add(FloatSlider("Probability / Decay", parameters.Probability));
            if (visible.HasFlag(TextureEffectUiParameter.Seed))
                controls.Add(FloatSlider("Seed", parameters.Seed));
            if (visible.HasFlag(TextureEffectUiParameter.Direction))
                controls.Add(UI.Field("Direction / Phase", () => source.EffectParams.Direction,
                    value => SetDirection(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.Grid))
                controls.Add(UI.Field("Grid", () => source.EffectParams.Grid, value => SetGrid(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.TilingScale))
                controls.Add(UI.Field("Tiling Scale", () => source.EffectParams.TileNum,
                    value => SetTilingScale(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.TilingOffset))
                controls.Add(UI.Field("Tiling Offset", () => source.EffectParams.Offset,
                    value => SetTilingOffset(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.PaletteThresholds))
                controls.Add(UI.Field("Palette Thresholds", () => source.EffectParams.Thresholds,
                    value => SetThresholds(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.Main))
                controls.Add(UI.Field("Main", () => source.EffectParams.Main, value => SetMain(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.Sub))
                controls.Add(UI.Field("Sub", () => source.EffectParams.Sub, value => SetSub(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.Base))
                controls.Add(UI.Field("Base", () => source.EffectParams.Base, value => SetBase(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.Accent))
                controls.Add(UI.Field("Accent", () => source.EffectParams.Accent, value => SetAccent(source, value)));

            return controls;
        }

        private static Element FloatSlider(string label, FloatParameter parameter)
        {
            return UI.Slider(label, () => parameter.Value, value => parameter.TrySetValue(value),
                parameter.Min, parameter.Max);
        }

        private static string ActiveEffectNames(TextureEffectStage stage, TextureEffectParameters parameters)
        {
            var names = new List<string>();
            foreach (var definition in TextureEffectUiMetadata.ForStage(stage))
                if (parameters.IsEffectEnabled(stage, definition.Value)) names.Add(definition.Label);
            return names.Count == 0 ? "—" : string.Join(", ", names);
        }

        private static string StageLabel(TextureEffectStage stage)
        {
            return stage switch
            {
                TextureEffectStage.Uv => "UV",
                TextureEffectStage.Sampling => "Sampling",
                TextureEffectStage.Color => "Color",
                _ => "Alpha"
            };
        }

        private ProcessedTextureSource SelectedSource => _selectedTarget switch
        {
            0 => _modelSource,
            1 => _backgroundSource,
            _ => _outputSource
        };

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

        private static void SetMain(ProcessedTextureSource source, Color value)
        {
            var effectParams = source.EffectParams;
            effectParams.Main = value;
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

        private static void SetSub(ProcessedTextureSource source, Color value)
        {
            var effectParams = source.EffectParams;
            effectParams.Sub = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetBase(ProcessedTextureSource source, Color value)
        {
            var effectParams = source.EffectParams;
            effectParams.Base = value;
            source.SetEffectParams(effectParams);
        }

        private static void SetAccent(ProcessedTextureSource source, Color value)
        {
            var effectParams = source.EffectParams;
            effectParams.Accent = value;
            source.SetEffectParams(effectParams);
        }
    }
}