using System.Collections.Generic;
using System.Linq;
using RosettaUI;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Scripts.View
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
            var launchers = _targets.Select(t =>
            {
                var window = UI.Window("どうにかしたい箇所", t.CreateElement());
                return UI.WindowLauncher(window);
            });

            return UI.Window("Qoooo", UI.Column(launchers));
        }
    }
}
