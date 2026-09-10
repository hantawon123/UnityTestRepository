using Game.Client;
using Game.Core.Settings;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// Hands the 컨트롤 tab's applied keys to the on-screen guide for as
    /// long as the application lives.
    /// </summary>
    public sealed class KeySettingGuideBinder : IStartable, System.IDisposable
    {
        private readonly ControlSettingsSystem settings;

        public KeySettingGuideBinder(ControlSettingsSystem settings) => this.settings = settings;

        public void Start() => KeySettingGuideView.UseSettings(settings);

        public void Dispose() => KeySettingGuideView.UseSettings(null);
    }
}
