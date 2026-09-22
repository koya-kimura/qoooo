using System.Collections.Generic;
using System.Linq;
using PrefsGUI;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using qoooo.Presentation.Contracts;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Presentation.Runtime
{
    public class UiBuilder : MonoBehaviour
    {
        private List<IUiTarget> _targets;
        private List<IMidiBindableParameter> _parameters;
        private List<IPrefsSaveParticipant> _saveParticipants;

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

        Element CreateElement()
        {
            var liveLaunchers = new List<Element>
            {
                CreateParameterWindow("Model", "model.source", "model.active", "model.motion", "model.scale",
                    "model.reset-transform"),
                CreateParameterWindow("Camera", "model.camera-pose"),
                CreateParameterWindow("Poster", "poster.layout", "poster.main-text"),
                CreateParameterWindow("Background / Palette", "background.pattern", "visual.palette"),
                CreateParameterWindow("Rhythm", "rhythm.bpm", "rhythm.tap", "rhythm.sync-beat", "rhythm.sync-bar")
            };
            liveLaunchers.AddRange(CreateTargetLaunchers("TextureCompositor"));

            var authoringLaunchers = CreateTargetLaunchers("PosterComposer");
            var setupLaunchers = CreateTargetLaunchers("OutputSettings", "MidiSystemController");

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

        private IMidiBindableParameter FindParameter(string id)
        {
            return (_parameters ?? new List<IMidiBindableParameter>())
                .FirstOrDefault(parameter => parameter.Id == id);
        }

        private static Element CreateParameterControl(IMidiBindableParameter parameter)
        {
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
                "TextureCompositor" => "Effects",
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
