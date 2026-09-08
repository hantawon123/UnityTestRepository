using System.Collections.Generic;
using Game.Core.Ports;
using Game.Core.Settings;
using UnityEngine;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Carries the 사운드 settings into Unity's audio.
    /// </summary>
    /// <remarks>
    /// One of the seven rows reaches the ear today: the master volume, which is
    /// <see cref="AudioListener.volume"/> and exists wherever the game runs.
    /// <para>
    /// The other six are not wired up here, and this class is deliberately the
    /// only place that says so. The three sub-volumes need mixer groups the
    /// project does not have — every sound currently plays straight into the
    /// listener — and the microphone's device, input mode and volume belong to
    /// the voice rig, which exists only inside a room and is spoken to through
    /// <see cref="IVoiceControl"/>, a port that today knows only how to mute.
    /// They are saved and shown correctly, and changing one has no effect on
    /// what is heard until those are added.
    /// </para>
    /// </remarks>
    public sealed class UnitySoundSettingsApplier : ISoundSettingsApplier
    {
        public void Apply(SoundSettings settings)
        {
            AudioListener.volume = Mathf.Clamp01(
                settings.Get(SoundVolume.Master) / (float)SoundCatalog.MaxVolume);
        }
    }

    /// <summary>The microphones Unity can see on this machine.</summary>
    public sealed class UnityMicrophoneDevices : IMicrophoneDevices
    {
        public IReadOnlyList<string> Names
        {
            get
            {
#if UNITY_WEBGL
                // The browser hands out microphones through its own permission
                // prompt, which Unity's Microphone class does not go through.
                return System.Array.Empty<string>();
#else
                return Microphone.devices ?? System.Array.Empty<string>();
#endif
            }
        }
    }

    /// <summary>
    /// Makes the saved sound settings heard when the game starts.
    /// </summary>
    /// <inheritdoc cref="GraphicsSettingsStartup"/>
    public sealed class SoundSettingsStartup : IStartable
    {
        private readonly SoundSettingsSystem sound;

        public SoundSettingsStartup(SoundSettingsSystem sound)
        {
            this.sound = sound;
        }

        public void Start() => sound.ApplyToAudio();
    }
}
