namespace Game.Client.Settings
{
    public interface ISettingsEdit
    {
        bool HasChanges { get; }
        void BeginEdit();
        void ApplyEdit();
        void ResetEdit();
        void CancelEdit();
    }
}
