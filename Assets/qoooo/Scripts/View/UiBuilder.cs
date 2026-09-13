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

        [Inject]
        public void Constructs(IEnumerable<IUiTarget> targets)
        {
            _targets = targets.ToList();
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
                    UI.Button("Save", Prefs.Save)
                    )
               );
        }
    }
}
