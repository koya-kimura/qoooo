using UnityEngine;

namespace qoooo.Interfaces
{
    public interface ITextureContainer
    {
        public Texture Texture { get; }
        public TextureUsage Usage { get; }
    }
}
