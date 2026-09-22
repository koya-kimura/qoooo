using UnityEngine;

namespace qoooo.Rendering.Composition
{
    // Read-only output-resolution boundary used by render-target owners.
    public interface IResolutionContainer
    {
        Vector2Int Resolution { get; }
    }
}
