using System.Collections.Generic;
using System.Linq;
using qoooo.Interfaces;
using UnityEngine;
using UnitySimpleContainer;

namespace qoooo.Stores
{
    public class TextureStore
    {
        private List<ITextureContainer> _containers;

        [Inject]
        public void Constructs(IEnumerable<ITextureContainer> containers)
        {
            _containers = containers.ToList();
        }

        public List<Texture> Textures()
        {
            return _containers
                .Select(container => container.Texture)
                .Where(texture => texture != null)
                .ToList();
        }

        public List<Texture> Textures(TextureUsage usage)
        {
            return _containers
                .Where(container => container.Usage == usage)
                .Select(container => container.Texture)
                .Where(texture => texture != null)
                .ToList();
        }

        public Texture ModelTexture(ModelTextureId id)
        {
            return _containers
                .OfType<IModelTextureContainer>()
                .FirstOrDefault(container => container.ModelTextureId == id)
                ?.Texture;
        }

        public List<Texture> ModelTextures()
        {
            return _containers
                .OfType<IModelTextureContainer>()
                .OrderBy(container => container.ModelTextureId)
                .Select(container => container.Texture)
                .Where(texture => texture != null)
                .ToList();
        }
    }
}
