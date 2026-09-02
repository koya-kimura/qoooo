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

        [SerializeField] private PanelSettings panelSettings;
        [SerializeField] private MonoBehaviour settingsControllerSource;
        [SerializeField] private MonoBehaviour preferencesSaverSource;

        private IVjOutputSettingsController _settingsController;
        private IVjPreferencesSaver _preferencesSaver;
        private RosettaUIRootUIToolkit _root;
        private UIDocument _document;
        private GameObject _uiRootObject;
        private VjPreferences _draft = new();
        private string _status = "Ready";
        private bool _built;
        private WindowElement _outputWindow;

        public bool IsOutputWindowOpen => _outputWindow?.IsOpen == true;
        public PanelSettings PanelSettings
        {
            get => panelSettings;
            set => panelSettings = value;
        }

        public void Initialize(
            IVjOutputSettingsController settingsController,
            IVjPreferencesSaver preferencesSaver)
        {
            if (settingsController == null) return;
            _settingsController = settingsController;
            _preferencesSaver = preferencesSaver;
            _draft = settingsController.CurrentPreferences.Copy();
        }

        private void Start()
        {
            if (_settingsController == null)
            {
                Initialize(
                    settingsControllerSource as IVjOutputSettingsController,
                    preferencesSaverSource as IVjPreferencesSaver);
            }

            EnsureRosettaRoot();
            if (_built) return;

            _root.Build(CreateLauncher());
            _outputWindow.Open(recursive: true);
            _built = true;
        }

        private void OnDestroy()
        {
            VjRuntimePanelFactory.DestroyOwned(_uiRootObject);
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

            _outputWindow = RUI.Window(
                    "Output Settings",
                    RUI.Page(
                        textureSettings,
                        senderSettings,
                        RUI.Button("Apply", ApplyDraft)))
                .SetPosition(new Vector2(24f, 64f));

            return RUI.Row(
                RUI.WindowLauncher("Output", _outputWindow),
                RUI.Button("Save Prefs", SaveDraft),
                RUI.Label(() => _status));
        }

        private void ApplyDraft()
        {
            if (_settingsController == null)
            {
                _status = "Application is not connected";
                return;
            }

            _settingsController.ApplyPreferences(_draft);
            _draft = _settingsController.CurrentPreferences.Copy();
            _status = "Applied";
        }

        private void SaveDraft()
        {
            if (_preferencesSaver == null)
            {
                _status = "Application is not connected";
                return;
            }

            _preferencesSaver.SavePreferences(_draft);
            _draft = _settingsController.CurrentPreferences.Copy();
            _status = "Saved to PlayerPrefs";
        }

        private void EnsureRosettaRoot()
        {
            _document = VjRuntimePanelFactory.CreateDocument(
                transform,
                "VJ RosettaUI",
                panelSettings,
                ControlsSortingOrder,
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
