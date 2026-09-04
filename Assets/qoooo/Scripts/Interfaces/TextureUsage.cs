namespace qoooo.Interfaces
{
    public enum TextureUsage
    {
        Default,
        Model,
        Text,
        Background,
        Effect
    }

    public enum ModelTextureId
    {
        Front,
        Side,
        Top,
        Count
    }

    public interface IModelTextureContainer : ITextureContainer
    {
        ModelTextureId ModelTextureId { get; }
    }
}
