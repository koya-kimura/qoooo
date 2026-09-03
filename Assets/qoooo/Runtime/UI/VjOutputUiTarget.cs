using System;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;
using RUI = RosettaUI.UI;

namespace Qoooo.VJ.UI
{
    [DisallowMultipleComponent]
    public sealed class VjOutputUiTarget : MonoBehaviour, IUiTarget
    {
        private static readonly string[] OutputModeNames = Enum.GetNames(typeof(VjOutputMode));

        private IVjOutputSettingsController _settingsController;
        private VjPreferences _draft = new();
        private Element _rootUI;

        public int Order => 100;
        public Element RootUI => _rootUI ??= RUI.WindowLauncher("Output", Window);
        public WindowElement Window { get; private set; }

        [Inject]
        public void Construct(IVjOutputSettingsController settingsController)
        {
            _settingsController = settingsController;
            _draft = settingsController.CurrentPreferences.Copy();
            BuildWindow();
        }

        public void Initialize(IVjOutputSettingsController settingsController)
            => Construct(settingsController);

        private void BuildWindow()
        {
            if (Window != null) return;

            var textureSettings = RUI.Fold(
                    "Final Texture",
                    RUI.Field("Width", () => _draft.outputWidth, value =>
                        _draft.outputWidth = Mathf.Clamp(
                            value,
                            VjRuntimeSettings.MinDimension,
                            VjRuntimeSettings.MaxDimension)),
                    RUI.Field("Height", () => _draft.outputHeight, value =>
                        _draft.outputHeight = Mathf.Clamp(
                            value,
                            VjRuntimeSettings.MinDimension,
                            VjRuntimeSettings.MaxDimension)))
                .Open();

            var senderSettings = RUI.Fold(
                    "Sender",
                    RUI.Field("Output Name", () => _draft.outputName, value =>
                        _draft.outputName = value),
                    RUI.Dropdown(
                        "Output Mode",
                        () => (int)_draft.outputMode,
                        value => _draft.outputMode = (VjOutputMode)value,
                        OutputModeNames))
                .Open();

            Window = RUI.Window(
                    "Output Settings",
                    RUI.Page(
                        textureSettings,
                        senderSettings,
                        RUI.Button("Apply", ApplyDraft)))
                .SetPosition(new Vector2(24f, 112f));
        }

        private void ApplyDraft()
        {
            if (_settingsController == null) return;
            _settingsController.ApplyPreferences(_draft);
            _draft = _settingsController.CurrentPreferences.Copy();
        }
    }
}
