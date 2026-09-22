using RosettaUI;

namespace qoooo.Presentation.Contracts
{
    // Supplies RosettaUI presentation for an existing owner without transferring application state.
    public interface IUiTarget
    {
        public Element CreateElement();
    }
}
