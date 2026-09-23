using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Presentation.Contracts;
using RosettaUI;
using UnityEngine;

namespace qoooo.Rendering.Composition
{
    // Effect値はsourceが所有し、このUIはRackと単一Inspectorのnavigation stateだけを持つ。
    public sealed class TextureProcessingUi : MonoBehaviour, IUiTarget
    {
        private static readonly string[] TargetNames = { "Model", "Background" };

        [SerializeField] private ProcessedTextureSource _modelSource;
        [SerializeField] private ProcessedTextureSource _backgroundSource;

        // TargetはTexture処理の値ではなく、このWindow内だけのnavigation state。
        private int _selectedTarget;
        private int _inspectorTarget;
        private TextureEffectStage _inspectorStage;
        private int _inspectorEffect;
        private bool _inspectorOpen;

        public Element CreateElement()
        {
            return UI.Column(
                UI.Dropdown("Target", () => _selectedTarget, SetSelectedTarget, TargetNames),
                UI.DynamicElementOnStatusChanged(
                    () => _selectedTarget,
                    _ => CreateEffectsElement()),
                UI.DynamicElementOnStatusChanged(
                    InspectorStatus,
                    _ => CreateInspectorElement()));
        }

        private Element CreateEffectsElement()
        {
            var source = SelectedSource;
            if (source == null) return UI.HelpBox("Selected texture source is not assigned.");
            var parameters = source.EffectParameters;

            return UI.Column(
                UI.Fold("Active Pipeline",
                    UI.Label(() => $"1. UV        {ActiveEffectName(TextureEffectStage.Uv, parameters.UvMode.Value)}"),
                    UI.Label(() => $"2. Sampling  {ActiveEffectName(TextureEffectStage.Sampling, parameters.SamplingMode.Value)}"),
                    UI.Label(() => $"3. Color     {ActiveEffectName(TextureEffectStage.Color, parameters.ColorMode.Value)}"),
                    UI.Label(() => $"4. Alpha     {ActiveEffectName(TextureEffectStage.Alpha, parameters.AlphaMode.Value)}")
                ).Open(),
                UI.Fold("Effects",
                    CreateStageRack("UV", TextureEffectStage.Uv, parameters.UvMode),
                    CreateStageRack("Sampling", TextureEffectStage.Sampling, parameters.SamplingMode),
                    CreateStageRack("Color", TextureEffectStage.Color, parameters.ColorMode),
                    CreateStageRack("Alpha", TextureEffectStage.Alpha, parameters.AlphaMode)
                ).Open());
        }

        private Element CreateStageRack(string label, TextureEffectStage stage, RadioParameter parameter)
        {
            var rows = new List<Element>();
            foreach (var definition in TextureEffectUiMetadata.ForStage(stage))
            {
                var selected = definition;
                rows.Add(UI.Row(
                    UI.Field(null,
                        () => parameter.Value == selected.Value,
                        active => SetEffectActive(parameter, selected.Value, active)),
                    UI.Button(selected.Label, () => OpenInspector(selected)),
                    UI.Button("Edit", () => OpenInspector(selected))));
            }

            return UI.Fold(label, UI.Column(rows));
        }

        private Element CreateInspectorElement()
        {
            if (!_inspectorOpen)
                return UI.Fold("Effect Inspector", UI.HelpBox("Choose an effect's Edit button."));

            var source = SourceForTarget(_inspectorTarget);
            var definition = TextureEffectUiMetadata.Find(_inspectorStage, _inspectorEffect);
            if (source == null || !definition.HasValue)
                return UI.Fold("Effect Inspector", UI.HelpBox("The selected effect is unavailable."));

            var selected = definition.Value;
            var controls = CreateInspectorControls(source, selected.Parameters);
            controls.Insert(0, UI.Label($"Target: {TargetNames[_inspectorTarget]}"));
            controls.Insert(1, UI.Label($"Stage: {StageLabel(selected.Stage)}"));
            controls.Insert(2, UI.Label($"Effect: {selected.Label}"));
            controls.Add(UI.Button("Close", CloseInspector));
            return UI.Fold("Effect Inspector", UI.Column(controls)).Open();
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
            if (visible.HasFlag(TextureEffectUiParameter.ColorA))
                controls.Add(UI.Field("Color A", () => source.EffectParams.ColorA, value => SetColorA(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.ColorB))
                controls.Add(UI.Field("Color B", () => source.EffectParams.ColorB, value => SetColorB(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.ColorC))
                controls.Add(UI.Field("Color C", () => source.EffectParams.ColorC, value => SetColorC(source, value)));
            if (visible.HasFlag(TextureEffectUiParameter.ColorD))
                controls.Add(UI.Field("Color D", () => source.EffectParams.ColorD, value => SetColorD(source, value)));

            return controls;
        }

        private static Element FloatSlider(string label, FloatParameter parameter)
        {
            return UI.Slider(label, () => parameter.Value, value => parameter.TrySetValue(value),
                parameter.Min, parameter.Max);
        }

        private static void SetEffectActive(RadioParameter parameter, int value, bool active)
        {
            if (active) parameter.TrySetValue(value);
            else if (parameter.Value == value) parameter.TrySetValue(0);
        }

        private void OpenInspector(TextureEffectUiDefinition definition)
        {
            _inspectorTarget = _selectedTarget;
            _inspectorStage = definition.Stage;
            _inspectorEffect = definition.Value;
            _inspectorOpen = true;
        }

        private void CloseInspector()
        {
            _inspectorOpen = false;
        }

        private string InspectorStatus()
        {
            return $"{_inspectorOpen}:{_inspectorTarget}:{(int)_inspectorStage}:{_inspectorEffect}";
        }

        private static string ActiveEffectName(TextureEffectStage stage, int value)
        {
            if (value == 0) return "—";
            return TextureEffectUiMetadata.Find(stage, value)?.Label ?? "Unknown";
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

        private ProcessedTextureSource SelectedSource => _selectedTarget == 0 ? _modelSource : _backgroundSource;

        private ProcessedTextureSource SourceForTarget(int target)
        {
            return target == 0 ? _modelSource : _backgroundSource;
        }

        private void SetSelectedTarget(int target)
        {
            _selectedTarget = Mathf.Clamp(target, 0, TargetNames.Length - 1);
            if (_inspectorOpen) _inspectorTarget = _selectedTarget;
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