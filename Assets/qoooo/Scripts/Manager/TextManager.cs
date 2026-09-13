using System.Collections.Generic;
using qoooo.Controller;
using UnityEngine;

namespace qoooo.Manager
{
    public class TextManager : MonoBehaviour
    {
        [SerializeField] private List<TextController> _texts = new();
        [SerializeField] private int _selectedIndex;

        public TextController Selected
        {
            get
            {
                if (_texts.Count == 0) return null;
                return _texts[Mathf.Clamp(_selectedIndex, 0, _texts.Count - 1)];
            }
        }

        public void Select(int index)
        {
            if (_texts.Count == 0)
            {
                _selectedIndex = 0;
                return;
            }

            _selectedIndex = Mathf.Clamp(index, 0, _texts.Count - 1);
        }

        public void SetText(string value)
        {
            Selected?.SetText(value);
        }

        public void SelectPattern(int index)
        {
            Selected?.SelectPattern(index);
        }
    }
}
