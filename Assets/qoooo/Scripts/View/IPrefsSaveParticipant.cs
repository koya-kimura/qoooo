namespace qoooo.View
{
    /// <summary>既存のSaveボタンに、Prefsへ保存すべきruntime状態を渡す。</summary>
    public interface IPrefsSaveParticipant
    {
        void PrepareSave();
        void CommitSave();
        void AbortSave();
    }
}
