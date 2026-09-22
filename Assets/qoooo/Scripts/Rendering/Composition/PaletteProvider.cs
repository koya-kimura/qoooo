using System.Collections.Generic;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using UnityEngine;

namespace qoooo.Rendering.Composition
{
    // Read-only access to the shared palette selected by the provider's typed Parameter.
    public interface IPaletteProvider
    {
        Color Main { get; }
        Color Sub { get; }
    }

    // Owns the shared palette Parameter and exposes resolved colors to visual consumers.
    // DO NOT DUPLICATE palette state in individual layers.
    public class PaletteProvider : MonoBehaviour, IPaletteProvider, IMidiParameterContainer
    {
        [SerializeField]
        private RadioParameter _palette = new(
            "visual.palette",
            "Visual Palette",
            new[] { "Cobalt", "Acid", "Hot" });

        public Color Main => _palette.Value switch
        {
            1 => new Color(0.95f, 0.9f, 0.05f, 1f),
            2 => new Color(1f, 0.08f, 0.1f, 1f),
            _ => new Color(0.05f, 0.25f, 0.95f, 1f)
        };

        public Color Sub => _palette.Value switch
        {
            1 => new Color(0.2f, 1f, 0.1f, 1f),
            2 => new Color(1f, 0.2f, 0.55f, 1f),
            _ => Color.white
        };

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get { yield return _palette; }
        }

        private void Awake()
        {
            _palette ??= new RadioParameter(
                "visual.palette",
                "Visual Palette",
                new[] { "Cobalt", "Acid", "Hot" });
            _palette.ReplaceOptions(new[] { "Cobalt", "Acid", "Hot" });
        }
    }
}
