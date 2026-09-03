using System;
using Qoooo.VJ.Composition;
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
        [SerializeField] private CompositionController composition;

        private IVjOutputSettingsController _settingsController;
        private IVjPreferencesSaver _preferencesSaver;
        private RosettaUIRootUIToolkit _root;
        private UIDocument _document;
        private GameObject _uiRootObject;
        private VjPreferences _draft = new();
        private bool _built;
        private WindowElement _outputWindow;
        private VjLayerStackPanelBuilder _layerStack;
        private WindowElement _launcherWindow;
        private bool _isVisible = true;
#if ENABLE_INPUT_SYSTEM
        private InputAction _toggleUiAction;
#endif

        public bool IsOutputWindowOpen => _outputWindow?.IsOpen == true;
        public bool IsLayerStackWindowOpen => _layerStack?.Window.IsOpen == true;
        public bool IsLauncherWindowOpen => _launcherWindow?.IsOpen == true;
        public bool IsVisible => _isVisible;
        public PanelSettings PanelSettings
        {
            get => panelSettings;
            set => panelSettings = value;
        }

        public CompositionController Composition
        {
            get => composition;
            set => composition = value;
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
            _built = true;
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            _toggleUiAction = new InputAction("Toggle VJ UI", binding: "<Keyboard>/d");
            _toggleUiAction.performed += OnToggleUiPerformed;
            _toggleUiAction.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            if (_toggleUiAction == null) return;
            _toggleUiAction.performed -= OnToggleUiPerformed;
            _toggleUiAction.Disable();
            _toggleUiAction.Dispose();
            _toggleUiAction = null;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void OnToggleUiPerformed(InputAction.CallbackContext _) => ToggleVisible();
#endif

        public void ToggleVisible()
        {
            _isVisible = !_isVisible;
            if (_document != null)
            {
                _document.rootVisualElement.style.display =
                    _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
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
                .SetPosition(new Vector2(24f, 112f));

            _layerStack = composition != null
                ? new VjLayerStackPanelBuilder(composition)
                : null;

            var launcherContents = _layerStack != null
                ? RUI.Column(
                    RUI.WindowLauncher("Output", _outputWindow),
                    RUI.WindowLauncher("Layers", _layerStack.Window),
                    RUI.Button("Save Prefs", SaveDraft))
                : RUI.Column(
                    RUI.WindowLauncher("Output", _outputWindow),
                    RUI.Button("Save Prefs", SaveDraft));

            launcherContents.SetWidth(180f);

            _launcherWindow = RUI.Window("VJ Controls", launcherContents)
                .SetPosition(new Vector2(16f, 16f));

            return RUI.Column(
                    RUI.WindowLauncher("VJ", _launcherWindow).SetWidth(88f))
                .SetWidth(96f);
        }

        private void ApplyDraft()
        {
            if (_settingsController == null)
            {
                return;
            }

            _settingsController.ApplyPreferences(_draft);
            _draft = _settingsController.CurrentPreferences.Copy();
        }

        private void SaveDraft()
        {
            if (_preferencesSaver == null)
            {
                return;
            }

            _preferencesSaver.SavePreferences(_draft);
            _draft = _settingsController.CurrentPreferences.Copy();
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
