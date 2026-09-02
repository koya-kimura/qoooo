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
        private static readonly string[] OutputModeNames = Enum.GetNames(typeof(VjOutputMode));

        private IVjPreferencesController _controller;
        private RosettaUIRootUIToolkit _root;
        private UIDocument _document;
        private PanelSettings _ownedPanelSettings;
        private GameObject _uiRootObject;
        private VjPreferences _draft = new();
        private string _status = "Ready";
        private bool _built;

        public void Initialize(IVjPreferencesController controller)
        {
            _controller = controller;
            _draft = controller.CurrentPreferences.Copy();
        }

        private void Start()
        {
            EnsureRosettaRoot();
            if (_built) return;

            _root.Build(CreateUI());
            _built = true;
        }

        private void OnDestroy()
        {
            if (_uiRootObject != null)
            {
                if (Application.isPlaying)
                    Destroy(_uiRootObject);
                else
                    DestroyImmediate(_uiRootObject);
            }

            if (_ownedPanelSettings == null) return;

            if (Application.isPlaying)
                Destroy(_ownedPanelSettings);
            else
                DestroyImmediate(_ownedPanelSettings);
        }

        private Element CreateUI()
        {
            return RUI.Box(
                    RUI.Label("VJ Output"),
                    RUI.Field("Width", () => _draft.outputWidth, value =>
                        _draft.outputWidth = Mathf.Clamp(
                            value,
                            VjRuntimeSettings.MinDimension,
                            VjRuntimeSettings.MaxDimension)),
                    RUI.Field("Height", () => _draft.outputHeight, value =>
                        _draft.outputHeight = Mathf.Clamp(
                            value,
                            VjRuntimeSettings.MinDimension,
                            VjRuntimeSettings.MaxDimension)),
                    RUI.Field("Output Name", () => _draft.outputName, value =>
                        _draft.outputName = value),
                    RUI.Dropdown(
                        "Output Mode",
                        () => (int)_draft.outputMode,
                        value => _draft.outputMode = (VjOutputMode)value,
                        OutputModeNames),
                    RUI.Row(
                        RUI.Button("Apply", ApplyDraft),
                        RUI.Button("Save Prefs", SaveDraft),
                        RUI.Button("Load Prefs", LoadDraft)),
                    RUI.Label(() => _status))
                .SetWidth(360f);
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
            _uiRootObject = new GameObject("VJ RosettaUI");
            _uiRootObject.SetActive(false);
            _uiRootObject.transform.SetParent(transform, false);
            _document = _uiRootObject.AddComponent<UIDocument>();

            if (_document.panelSettings == null)
            {
                _ownedPanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                _ownedPanelSettings.name = "VJ Runtime Panel Settings";
                _ownedPanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                _ownedPanelSettings.referenceResolution = new Vector2Int(1920, 1080);
                _ownedPanelSettings.sortingOrder = 100;
                _ownedPanelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("VjRuntimeTheme");
                _document.panelSettings = _ownedPanelSettings;
            }

            _root = _uiRootObject.AddComponent<RosettaUIRootUIToolkit>();
#if ENABLE_INPUT_SYSTEM
            _root.undoAction = new InputAction("VJ UI Undo");
            _root.redoAction = new InputAction("VJ UI Redo");
#endif
            _uiRootObject.SetActive(true);
        }
    }
}
