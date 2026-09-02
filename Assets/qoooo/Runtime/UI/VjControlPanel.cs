using System;
using RosettaUI;
using RosettaUI.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;
using RUI = RosettaUI.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Qoooo.VJ.UI
{
    [DisallowMultipleComponent]
    public sealed class VjControlPanel : MonoBehaviour, IVjControlPanel
    {
        private const float ControlsSortingOrder = 100f;
        private static readonly string[] OutputModeNames = Enum.GetNames(typeof(VjOutputMode));

        private IVjPreferencesController _controller;
        private RosettaUIRootUIToolkit _root;
        private UIDocument _document;
        private PanelSettings _ownedPanelSettings;
        private GameObject _uiRootObject;
        private VjPreferences _draft = new();
        private string _status = "Ready";
        private bool _built;
        private WindowElement _outputWindow;

        public bool IsOutputWindowOpen => _outputWindow?.IsOpen == true;

        public void Initialize(IVjPreferencesController controller)
        {
            _controller = controller;
            _draft = controller.CurrentPreferences.Copy();
        }

        private void Start()
        {
            EnsureRosettaRoot();
            if (_built) return;

            _root.Build(CreateLauncher());
            _outputWindow.Open(recursive: true);
            _built = true;
        }

        private void OnDestroy()
        {
            VjRuntimePanelFactory.DestroyOwned(_uiRootObject, _ownedPanelSettings);
        }

        private Element CreateLauncher()
        {
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

            var preferenceActions = RUI.Fold(
                    "Preferences",
                    RUI.Row(
                        RUI.Button("Apply", ApplyDraft),
                        RUI.Button("Save Prefs", SaveDraft),
                        RUI.Button("Load Prefs", LoadDraft)),
                    RUI.Label(() => _status))
                .Open();

            _outputWindow = RUI.Window(
                    "Output Settings",
                    RUI.Page(textureSettings, senderSettings, preferenceActions))
                .SetPosition(new Vector2(24f, 64f));

            return RUI.Row(
                    RUI.WindowLauncher("Output", _outputWindow))
                .SetWidth(180f);
        }

        private void ApplyDraft()
        {
            if (_controller == null)
            {
                _status = "Application is not connected";
                return;
            }

            _controller.ApplyPreferences(_draft);
            _draft = _controller.CurrentPreferences.Copy();
            _status = "Applied";
        }

        private void SaveDraft()
        {
            if (_controller == null)
            {
                _status = "Application is not connected";
                return;
            }

            _controller.SavePreferences(_draft);
            _draft = _controller.CurrentPreferences.Copy();
            _status = "Saved to PlayerPrefs";
        }

        private void LoadDraft()
        {
            if (_controller != null && _controller.TryLoadPreferences(out var preferences))
            {
                _controller.ApplyPreferences(preferences);
                _draft = _controller.CurrentPreferences.Copy();
                _status = "Loaded from PlayerPrefs";
            }
            else
            {
                _status = "No saved preferences";
            }
        }

        private void EnsureRosettaRoot()
        {
            _document = VjRuntimePanelFactory.CreateDocument(
                transform,
                "VJ RosettaUI",
                ControlsSortingOrder,
                out _ownedPanelSettings,
                activate: false);
            _uiRootObject = _document.gameObject;

            _root = _uiRootObject.AddComponent<RosettaUIRootUIToolkit>();
#if ENABLE_INPUT_SYSTEM
            _root.undoAction = new InputAction("VJ UI Undo");
            _root.redoAction = new InputAction("VJ UI Redo");
#endif
            _uiRootObject.SetActive(true);
        }
    }
}
