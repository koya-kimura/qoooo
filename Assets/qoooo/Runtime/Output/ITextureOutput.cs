using UnityEngine;

namespace Qoooo.VJ.Output
{
    public interface ITextureOutput
    {
        bool IsAvailable { get; }
        void SetTexture(Texture texture);
        void Dispose();
    }
}
