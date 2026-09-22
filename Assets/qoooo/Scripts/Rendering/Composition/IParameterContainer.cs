using RosettaUI;

namespace qoooo.Rendering.Composition
{
    // Supplies editable presentation controls while the implementing component retains value ownership.
    public interface IParameterContainer
    {
        Element CreateParameterElement();
    }
}
