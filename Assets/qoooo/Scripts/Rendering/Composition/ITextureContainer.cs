using UnityEngine;

namespace qoooo.Rendering.Composition
{
    // Read-only texture-source boundary; implementations retain responsibility for texture lifetime.
    public interface ITextureContainer
    {
        Texture Texture { get; }
    }
}
