using System;
using System.Linq;
using Game.Client.Accessibility;
using Game.Client.Audio;
using Game.Client.Controls;
using Game.Client.Graphics;
using Game.Client.Home;
using Game.Core.Flow;
using Game.Core.Settings;
using VContainer.Unity;

namespace Game.Client.Settings
{
    public sealed class SettingsPresenter : IStartable, IDisposable
    {
        private readonly ISettingsView view;
        private ISettingsEdit[] edits = Array.Empty<ISettingsEdit>();
        private bool disposed;
        private readonly IHomeApplicationHost applicationHost;
        private readonly AppFlowSystem appFlow;
        private readonly IAudioSettings audioSettings;
        private readonly IAccessibilitySettings accessibilitySettings;
        private readonly IGraphicsSettings graphicsSettings;
        private readonly IControlSettings controlSettings;

        public SettingsPresenter(
            ISettingsView view,
            IHomeApplicationHost applicationHost,
            AppFlowSystem appFlow,
            IAudioSettings audioSettings,
            IAccessibilitySettings accessibilitySettings,
            IGraphicsSettings graphicsSettings,
            IControlSettings controlSettings)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.applicationHost = applicationHost
                ?? throw new ArgumentNullException(nameof(applicationHost));
            this.appFlow = appFlow ?? throw new ArgumentNullException(nameof(appFlow));
            this.audioSettings = audioSettings
                ?? throw new ArgumentNullException(nameof(audioSettings));
            this.accessibilitySettings = accessibilitySettings
                ?? throw new ArgumentNullException(nameof(accessibilitySettings));
            this.graphicsSettings = graphicsSettings
                ?? throw new ArgumentNullException(nameof(graphicsSettings));
            this.controlSettings = controlSettings
                ?? throw new ArgumentNullException(nameof(controlSettings));
        }

        public void Start()
        {
            edits = new object[] { audioSettings, accessibilitySettings, graphicsSettings, controlSettings }.OfType<ISettingsEdit>().ToArray();
            foreach (var edit in edits) edit.BeginEdit();
            view.SetEdited(false);
            view.ApplyRequested += Apply;
            view.ResetRequested += Reset;
            view.BackRequested += OnBackRequested;
            view.TabSelected += OnTabSelected;
            view.AudioVolumeChanged += OnAudioVolumeChanged;
            view.VoiceChatEnabledChanged += OnVoiceChatEnabledChanged;
            view.UiScaleChanged += OnUiScaleChanged;
            view.TextScaleChanged += OnTextScaleChanged;
            view.HighContrastChanged += OnHighContrastChanged;
            view.GraphicsSettingChanged += OnGraphicsSettingChanged;
            view.BrightnessChanged += OnBrightnessChanged;
            view.ControlRebindRequested += OnControlRebindRequested;
            audioSettings.Changed += OnAudioSettingsChanged;
            accessibilitySettings.Changed += OnAccessibilitySettingsChanged;
            graphicsSettings.Changed += OnGraphicsSettingsChanged;
            controlSettings.Changed += OnControlSettingsChanged;
            controlSettings.RebindListeningChanged += OnControlListeningChanged;
            controlSettings.BindingConflict += OnBindingConflict;
            view.SetActiveTab(SettingsTab.Graphics);
            view.SetAudioSettings(audioSettings.Current);
            view.SetAccessibilitySettings(accessibilitySettings.Current);
            view.SetGraphicsSettings(graphicsSettings.Current);
            view.SetControlSettings(controlSettings.Current);
        }

        public void Dispose()
        {
            disposed = true;
            view.ApplyRequested -= Apply;
            view.ResetRequested -= Reset;
            controlSettings.CancelRebind();
            view.BackRequested -= OnBackRequested;
            view.TabSelected -= OnTabSelected;
            view.AudioVolumeChanged -= OnAudioVolumeChanged;
            view.VoiceChatEnabledChanged -= OnVoiceChatEnabledChanged;
            view.UiScaleChanged -= OnUiScaleChanged;
            view.TextScaleChanged -= OnTextScaleChanged;
            view.HighContrastChanged -= OnHighContrastChanged;
            view.GraphicsSettingChanged -= OnGraphicsSettingChanged;
            view.BrightnessChanged -= OnBrightnessChanged;
            view.ControlRebindRequested -= OnControlRebindRequested;
            audioSettings.Changed -= OnAudioSettingsChanged;
            accessibilitySettings.Changed -= OnAccessibilitySettingsChanged;
            graphicsSettings.Changed -= OnGraphicsSettingsChanged;
            controlSettings.Changed -= OnControlSettingsChanged;
            controlSettings.RebindListeningChanged -= OnControlListeningChanged;
            controlSettings.BindingConflict -= OnBindingConflict;
            foreach (var edit in edits) edit.CancelEdit();
        }

        private void OnTabSelected(SettingsTab tab)
        {
            view.SetActiveTab(tab);
        }

        private void OnAudioVolumeChanged(AudioChannel channel, int percent)
        {
            audioSettings.TrySetVolume(channel, percent, out _);
        }

        private void OnVoiceChatEnabledChanged(bool enabled)
        {
            audioSettings.TrySetVoiceChatEnabled(enabled, out _);
        }

        private void OnUiScaleChanged(int percent)
        {
            accessibilitySettings.TrySetUiScale(percent, out _);
        }

        private void OnTextScaleChanged(int percent)
        {
            accessibilitySettings.TrySetTextScale(percent, out _);
        }

        private void OnHighContrastChanged(bool enabled)
        {
            accessibilitySettings.TrySetHighContrastEnabled(enabled, out _);
        }

        private void OnGraphicsSettingChanged(GraphicsSetting setting, int index)
        {
            switch (setting)
            {
                case GraphicsSetting.Quality:
                    graphicsSettings.TrySetQuality((GraphicsQualityPreset)index, out _);
                    return;
                case GraphicsSetting.Resolution:
                    graphicsSettings.TrySetResolution(index, out _);
                    return;
                case GraphicsSetting.DisplayMode:
                    graphicsSettings.TrySetDisplayMode((DisplayMode)index, out _);
                    return;
                case GraphicsSetting.FrameCap:
                    graphicsSettings.TrySetFrameCap(index, out _);
                    return;
                case GraphicsSetting.Shadows:
                    graphicsSettings.TrySetShadows((ShadowQualityLevel)index, out _);
                    return;
                case GraphicsSetting.Effects:
                    graphicsSettings.TrySetEffects((EffectsQualityLevel)index, out _);
                    return;
                case GraphicsSetting.AntiAliasing:
                    graphicsSettings.TrySetAntiAliasing((AntiAliasingMode)index, out _);
                    return;
            }
        }

        private void OnBrightnessChanged(int percent)
        {
            graphicsSettings.TrySetBrightness(percent, out _);
        }

        private void OnControlRebindRequested(ControlAction action)
        {
            view.SetControlMessage(string.Empty);
            controlSettings.TryStartRebind(action, out _);
        }

        private void OnAudioSettingsChanged(AudioSettingsState settings)
        {
            view.SetAudioSettings(settings);
            view.SetEdited(edits.Any(edit => edit.HasChanges));
        }

        private void OnAccessibilitySettingsChanged(AccessibilitySettingsState settings)
        {
            view.SetAccessibilitySettings(settings);
            view.SetEdited(edits.Any(edit => edit.HasChanges));
        }

        private void OnGraphicsSettingsChanged(GraphicsSettingsState settings)
        {
            view.SetGraphicsSettings(settings);
            view.SetEdited(edits.Any(edit => edit.HasChanges));
        }

        private void OnControlSettingsChanged(ControlSettingsState settings)
        {
            view.SetControlMessage(string.Empty);
            view.SetControlSettings(settings);
            view.SetEdited(edits.Any(edit => edit.HasChanges));
        }

        private void OnControlListeningChanged(ControlAction? action)
        {
            view.SetControlListening(action);
        }

        private void OnBindingConflict(ControlAction occupiedBy)
        {
            var label = ControlSettingsState.RowLabel(occupiedBy);
            view.SetControlMessage($"이미 '{label}'에 사용 중인 키입니다.");
        }

        private void Apply()
        {
            controlSettings.CancelRebind();
            foreach (var edit in edits) edit.ApplyEdit();
            view.SetEdited(false);
        }

        private void Reset()
        {
            if (!edits.Any(edit => edit.HasChanges)) return;
            controlSettings.CancelRebind();
            view.Confirm("설정을 기본값으로 초기화할까요? 적용하면 저장됩니다.", accepted =>
            {
                if (disposed || !accepted) return;
                foreach (var edit in edits) edit.ResetEdit();
                view.SetEdited(edits.Any(edit => edit.HasChanges));
            });
        }

        private void OnBackRequested()
        {
            controlSettings.CancelRebind();
            if (edits.Any(edit => edit.HasChanges))
            {
                view.Confirm("적용하지 않은 변경을 취소하고 나갈까요?", accepted =>
                {
                    if (!disposed && accepted) Exit();
                });
                return;
            }
            Exit();
        }

        private void Exit()
        {
            foreach (var edit in edits) edit.CancelEdit();
            controlSettings.CancelRebind();
            if (appFlow.CurrentState != AppFlowState.Home &&
                !appFlow.TryTransitionTo(AppFlowState.Home))
            {
                return;
            }

            applicationHost.OpenHome();
        }
    }
}
