using RosettaUI;

namespace Qoooo.VJ.UI
{
    public interface IUiTarget
    {
        int Order { get; }
        Element RootUI { get; }
        WindowElement Window { get; }
    }
}
