using System.Collections.Generic;
using System.Linq;
using PrefsGUI;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.View
{
    public class UiBuilder : MonoBehaviour
    {
        private List<IUiTarget> _targets;
        private List<IPrefsSaveParticipant> _saveParticipants;

        [Inject]
        public void Constructs(IEnumerable<IUiTarget> targets)
        {
            _targets = targets.ToList();
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
            var launchers = _targets.Select(target =>
            {
                var title = target is Component component
                    ? component.gameObject.name
                    : target.GetType().Name;
                var window = UI.Window(title, target.CreateElement());
                return UI.WindowLauncher(window);
            });

            return UI.Window("Qoooo",
                UI.Column(
                    UI.Column(launchers),
                    UI.Button("Save", SavePrefs)
                    )
               );
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
