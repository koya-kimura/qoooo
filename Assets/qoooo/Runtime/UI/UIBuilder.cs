using System.Collections.Generic;
using System.Linq;
using RosettaUI;
using RosettaUI.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;
using UnitySimpleContainer;
using RUI = RosettaUI.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Qoooo.VJ.UI
{
    [DisallowMultipleComponent]
    public sealed class UIBuilder : MonoBehaviour
    {
        private const float ControlsSortingOrder = 100f;

        [SerializeField] private PanelSettings panelSettings;

        private readonly Dictionary<IUiTarget, bool> _openStates = new();
        private IReadOnlyList<IUiTarget> _uiTargets = new List<IUiTarget>();
        private IVjOutputSettingsController _settingsController;
        private IVjPreferencesSaver _preferencesSaver;
        private RosettaUIRootUIToolkit _root;
        private UIDocument _document;
        private GameObject _uiRootObject;
        private WindowElement _rootWindow;
        private bool _built;
        private bool _isVisible = true;
#if ENABLE_INPUT_SYSTEM
        private InputAction _toggleUiAction;
#endif

        public bool IsVisible => _isVisible;
        public bool IsRootWindowOpen => _rootWindow?.IsOpen == true;
        public IReadOnlyList<IUiTarget> UiTargets => _uiTargets;
        public PanelSettings PanelSettings
        {
            get => panelSettings;
            set => panelSettings = value;
        }

        [Inject]
        public void Construct(
            IEnumerable<IUiTarget> uiTargets,
            IVjOutputSettingsController settingsController,
            IVjPreferencesSaver preferencesSaver)
        {
            _uiTargets = uiTargets.OrderBy(target => target.Order).ToList();
            _settingsController = settingsController;
            _preferencesSaver = preferencesSaver;
        }

        public void Initialize(
            IEnumerable<IUiTarget> uiTargets,
            IVjOutputSettingsController settingsController,
            IVjPreferencesSaver preferencesSaver)
            => Construct(uiTargets, settingsController, preferencesSaver);

        private void Start()
        {
            EnsureRosettaRoot();
            if (_built) return;

            _root.Build(CreateRootWindow());
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
            if (_root == null) return;

            if (_isVisible)
            {
                _root.enabled = true;
                foreach (var target in _uiTargets)
                    if (_openStates.TryGetValue(target, out var wasOpen) && wasOpen)
                        target.Window.Open();
                return;
            }

            _openStates.Clear();
            foreach (var target in _uiTargets)
            {
                _openStates[target] = target.Window.IsOpen;
                target.Window.Close();
            }
            _root.enabled = false;
        }

        private void OnDestroy()
        {
            VjRuntimePanelFactory.DestroyOwned(_uiRootObject);
        }

        private Element CreateRootWindow()
        {
            var elements = _uiTargets.Select(target => target.RootUI).ToList();
            elements.Add(RUI.Button("Save Prefs", SavePreferences));

            var contents = RUI.Column(elements);
            contents.SetWidth(180f);

            _rootWindow = RUI.Window("VJ Controls", contents)
                .SetClosable(false)
                .SetPosition(new Vector2(16f, 16f));
            return _rootWindow;
        }

        private void SavePreferences()
        {
            if (_preferencesSaver == null || _settingsController == null) return;
            _preferencesSaver.SavePreferences(_settingsController.CurrentPreferences);
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
