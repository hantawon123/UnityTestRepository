using System;
using Game.Core.Settings;

namespace Game.Client.Audio
{
    public sealed class AudioSettingsService : IAudioSettings, Game.Client.Settings.ISettingsEdit
    {
        private readonly IAudioSettingsStore store;
        private readonly IAudioSettingsApplier applier;

        public AudioSettingsService(
            IAudioSettingsStore store,
            IAudioSettingsApplier applier)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.applier = applier ?? throw new ArgumentNullException(nameof(applier));
            Current = store.LoadOrDefault() ?? new AudioSettingsState();
            ApplyCurrent();
        }

        public AudioSettingsState Current { get; private set; }

        public event Action<AudioSettingsState> Changed;

        public bool TrySetVolume(
            AudioChannel channel,
            int percent,
            out AudioSettingsError error)
        {
            if (!Enum.IsDefined(typeof(AudioChannel), channel))
            {
                error = AudioSettingsError.InvalidChannel;
                return false;
            }

            if (Current.GetPercent(channel) == percent)
            {
                error = AudioSettingsError.None;
                return true;
            }

            if (!Current.TrySetVolume(channel, percent, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TrySetVoiceChatEnabled(bool enabled, out AudioSettingsError error)
        {
            if (Current.VoiceChatEnabled == enabled)
            {
                error = AudioSettingsError.None;
                return true;
            }

            if (!Current.TrySetVoiceChatEnabled(enabled, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }


        private AudioSettingsState original;
        public bool HasChanges => original != null && !Same(Current, original);
        private static AudioSettingsState Copy(AudioSettingsState s) => new AudioSettingsState(s.MasterVolume, s.BgmVolume, s.SfxVolume, s.UiVolume, s.VoiceChatEnabled, s.VoiceVolume, s.MicVolume);
        private static bool Same(AudioSettingsState a, AudioSettingsState b) => a.MasterVolume == b.MasterVolume && a.BgmVolume == b.BgmVolume && a.SfxVolume == b.SfxVolume && a.UiVolume == b.UiVolume && a.VoiceChatEnabled == b.VoiceChatEnabled && a.VoiceVolume == b.VoiceVolume && a.MicVolume == b.MicVolume;
        public void BeginEdit()
        {
            if (original != null) return;
            original = Copy(Current);
            Current = Copy(Current);
        }
        public void ApplyEdit()
        {
            store.Save(Copy(Current));
            original = Copy(Current);
        }
        public void ResetEdit()
        {

            Current = new AudioSettingsState();
            ApplyCurrent();
            Changed?.Invoke(Current);
        }
        public void CancelEdit()
        {

            if (original == null) return;
            Current = original;
            original = null;
            ApplyCurrent();
            Changed?.Invoke(Current);
        }

        private void PersistAndApply()
        {
            if (original == null) store.Save(Copy(Current));
            ApplyCurrent();
            Changed?.Invoke(Current);
        }

        private void ApplyCurrent()
        {
            applier.Apply(Current);
            AudioSettingsOutput.Publish(Current);
        }
    }
}
