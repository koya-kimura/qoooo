using System.Collections.Generic;
using System.Linq;
using PrefsGUI;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Presentation.Contracts;
using qoooo.Rendering.Composition;
using qoooo.Rendering.Models;
using RosettaUI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnitySimpleContainer;

namespace qoooo.Presentation.Runtime
{
    // Composes LIVE, AUTHORING, and SETUP RosettaUI from existing targets.
    // It owns no application values and coordinates only explicit Qoooo Save transactions.
    public class UiBuilder : MonoBehaviour, IMidiParameterContainer
    {
        private List<IUiTarget> _targets;
        private List<IMidiBindableParameter> _parameters;
        private List<IPrefsSaveParticipant> _saveParticipants;
        [SerializeField] private OneshotParameter _toggleVisibility = new("ui.toggle-visibility", "Toggle UI Visibility");
        private UIDocument _uiDocument;
        private bool _isUiVisible = true;
        private bool _saveShortcutWasPressed;

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get { yield return _toggleVisibility; }
        }

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            _toggleVisibility.Changed += OnToggleVisibilityChanged;
        }

        private void OnDestroy()
        {
            if (_toggleVisibility != null)
                _toggleVisibility.Changed -= OnToggleVisibilityChanged;
        }

        [Inject]
        public void Constructs(IEnumerable<IUiTarget> targets)
        {
            _targets = targets.ToList();
        }

        [Inject]
        public void ConstructParameters(IEnumerable<IMidiParameterContainer> containers)
        {
            _parameters = containers
                .SelectMany(container => container.MidiParameters ?? Enumerable.Empty<IMidiBindableParameter>())
                .Where(parameter => parameter != null)
                .GroupBy(parameter => parameter.Id)
                .Select(group => group.First())
                .ToList();
        }

        [Inject]
        public void ConstructSaveParticipants(IEnumerable<IPrefsSaveParticipant> participants)
        {
            _saveParticipants = participants.ToList();
        }

        void Start()
        {
            var root = GetComponent<RosettaUIRoot>();
            root.Build(CreateElement());
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var saveModifierPressed = Application.platform is RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer
                ? keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed
                : keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            var saveShortcutPressed = saveModifierPressed && keyboard.sKey.isPressed;
            if (saveShortcutPressed && !_saveShortcutWasPressed) SavePrefs();
            _saveShortcutWasPressed = saveShortcutPressed;
        }

        private void OnToggleVisibilityChanged(bool triggered)
        {
            if (!triggered) return;

            _isUiVisible = !_isUiVisible;
            if (_uiDocument?.rootVisualElement != null)
                _uiDocument.rootVisualElement.visible = _isUiVisible;
        }

        Element CreateElement()
        {
            var liveLaunchers = new List<Element>
            {
                CreateModelWindow(),
                CreateParameterWindow("Camera", "model.camera-pose"),
                CreateParameterWindow("Poster", "poster.layout", "poster.main-text"),
                CreateBackgroundWindow(),
                CreateParameterWindow("Rhythm", "rhythm.bpm", "rhythm.tap", "rhythm.sync-beat", "rhythm.sync-bar")
            };
            liveLaunchers.InsertRange(4, CreateTargetLaunchers("LiveCameraSource"));
            liveLaunchers.InsertRange(4, CreateTargetLaunchers("TextureProcessingUi"));

            var authoringLaunchers = CreateTargetLaunchers("PosterComposer");
            var setupLaunchers = CreateTargetLaunchers("OutputSettings", "TextureCompositor", "MidiSystemController");

            return UI.Window("Qoooo",
                UI.Column(
                    UI.Label("LIVE CONTROLS"),
                    UI.Column(liveLaunchers),
                    UI.Label("AUTHORING TOOLS"),
                    UI.Column(authoringLaunchers),
                    UI.Label("SETUP / DEBUG"),
                    UI.Column(setupLaunchers),
                    UI.Button("Save", SavePrefs)
                    )
               );
        }

        private IEnumerable<Element> CreateTargetLaunchers(params string[] typeNames)
        {
            return (_targets ?? new List<IUiTarget>())
                .Where(target => typeNames.Contains(target.GetType().Name))
                .OrderBy(target => System.Array.IndexOf(typeNames, target.GetType().Name))
                .Select(target => UI.WindowLauncher(UI.Window(TargetTitle(target), target.CreateElement())));
        }

        private Element CreateParameterWindow(string title, params string[] parameterIds)
        {
            var controls = parameterIds
                .Select(FindParameter)
                .Where(parameter => parameter != null)
                .Select(CreateParameterControl)
                .ToList();
            return UI.WindowLauncher(UI.Window(title, UI.Column(controls)));
        }

        private Element CreateModelWindow()
        {
            var controls = new List<Element>
            {
                CreateParameterControl(FindParameter("model.source")),
                CreateParameterControl(FindParameter("model.active")),
                CreateParameterControl(FindParameter("model.motion")),
                CreateParameterControl(FindParameter("model.scale")),
                CreateParameterControl(FindParameter("model.reset-transform"))
            };
            controls.RemoveAll(control => control == null);

            var arrangement = (_targets ?? new List<IUiTarget>()).OfType<ModelArrangementController>().FirstOrDefault();
            if (arrangement != null) controls.Add(arrangement.CreateElement());

            var animationMaster = new[]
                {
                    "model.animation-enabled",
                    "model.animation-speed",
                    "model.animation-strength"
                }
                .Select(FindParameter)
                .Where(parameter => parameter != null)
                .Select(CreateParameterControl)
                .ToArray();
            if (animationMaster.Length > 0)
                controls.Add(UI.Fold("Animation Master", UI.Column(animationMaster)).Open());

            var model = (_targets ?? new List<IUiTarget>()).OfType<ModelController>().FirstOrDefault();
            if (model != null) controls.Add(model.CreateElement());
            return UI.WindowLauncher(UI.Window("Model", UI.Column(controls)));
        }

        private Element CreateBackgroundWindow()
        {
            var controls = new List<Element>();
            var backgroundPattern = CreateParameterControl(FindParameter("background.pattern"));
            if (backgroundPattern != null) controls.Add(backgroundPattern);

            var palette = (_targets ?? new List<IUiTarget>()).OfType<PaletteProvider>().FirstOrDefault();
            if (palette != null) controls.Add(palette.CreateElement(SavePrefs));
            return UI.WindowLauncher(UI.Window("Background / Palette", UI.Column(controls)));
        }

        private IMidiBindableParameter FindParameter(string id)
        {
            return (_parameters ?? new List<IMidiBindableParameter>())
                .FirstOrDefault(parameter => parameter.Id == id);
        }

        private static Element CreateParameterControl(IMidiBindableParameter parameter)
        {
            if (parameter == null) return null;
            var label = ParameterLabel(parameter);
            return parameter switch
            {
                BoolParameter value => UI.Field(label, () => value.Value, next => value.TrySetValue(next)),
                FloatParameter value => UI.Slider(label, () => value.Value, next => value.TrySetValue(next),
                    value.Min, value.Max),
                RadioParameter value => UI.Dropdown(label, () => value.Value, next => value.TrySetValue(next),
                    value.Options.ToArray()),
                OneshotParameter value => UI.Button(label, value.Trigger),
                MomentaryParameter value => UI.Button(label, value.Trigger),
                StateParameter value => UI.Button(
                    UI.Label(() => $"{label}: {value.Value + 1}/{value.StateCount}"), () => value.Next()),
                SequenceParameter value => UI.Row(
                    Enumerable.Range(0, value.StepCount)
                        .Select(index => UI.Button(
                            UI.Label(() => value.Steps[index] ? $"{index + 1}: ON" : $"{index + 1}: OFF"),
                            () => value.ToggleStep(index)))
                        .ToList()),
                _ => UI.Label(label)
            };
        }

        private static string ParameterLabel(IMidiBindableParameter parameter)
        {
            return parameter.Id switch
            {
                "rhythm.bpm" => "BPM",
                "rhythm.tap" => "Tap",
                "rhythm.sync-beat" => "Sync Beat",
                "rhythm.sync-bar" => "Sync Bar",
                _ => parameter.DisplayName
            };
        }

        private static string TargetTitle(IUiTarget target)
        {
            return target.GetType().Name switch
            {
                "TextureProcessingUi" => "Texture",
                "TextureCompositor" => "Layers",
                "PosterComposer" => "Poster Composer",
                "OutputSettings" => "Output Setup",
                "MidiSystemController" => "MIDI Setup",
                _ => target is Component component ? component.gameObject.name : target.GetType().Name
            };
        }

        private void SavePrefs()
        {
            try
            {
                foreach (var participant in _saveParticipants ?? new List<IPrefsSaveParticipant>()) participant.PrepareSave();
                Prefs.Save();
                foreach (var participant in _saveParticipants ?? new List<IPrefsSaveParticipant>()) participant.CommitSave();
            }
            catch (System.Exception exception)
            {
                foreach (var participant in _saveParticipants ?? new List<IPrefsSaveParticipant>()) participant.AbortSave();
                Debug.LogError($"[Prefs] Save failed: {exception}", this);
            }
        }
    }
}
