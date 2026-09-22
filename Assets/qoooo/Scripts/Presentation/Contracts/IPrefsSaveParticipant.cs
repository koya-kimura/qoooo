namespace qoooo.Presentation.Contracts
{
    /// <summary>既存のSaveボタンに、Prefsへ保存すべきruntime状態を渡す。</summary>
    // Participates in explicit Qoooo Save: stage in Prepare, accept in Commit, recover in Abort.
    // Implementations are not invoked as autosave when Play Mode stops.
    public interface IPrefsSaveParticipant
    {
        void PrepareSave();
        void CommitSave();
        void AbortSave();
    }
}
