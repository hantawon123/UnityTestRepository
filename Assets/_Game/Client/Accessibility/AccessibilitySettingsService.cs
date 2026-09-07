using System;
using Game.Core.Settings;

namespace Game.Client.Accessibility
{
    public sealed class AccessibilitySettingsService : IAccessibilitySettings, Game.Client.Settings.ISettingsEdit
    {
        private readonly IAccessibilitySettingsStore store;
        private readonly IAccessibilitySettingsApplier applier;

        public AccessibilitySettingsService(
            IAccessibilitySettingsStore store,
            IAccessibilitySettingsApplier applier)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.applier = applier ?? throw new ArgumentNullException(nameof(applier));
            Current = store.LoadOrDefault() ?? new AccessibilitySettingsState();
            ApplyCurrent();
        }

        public AccessibilitySettingsState Current { get; private set; }

        public event Action<AccessibilitySettingsState> Changed;

        public bool TrySetUiScale(int percent, out AccessibilitySettingsError error)
        {
            if (Current.UiScale == percent)
            {
                error = AccessibilitySettingsError.None;
                return true;
            }

            if (!Current.TrySetUiScale(percent, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TrySetTextScale(int percent, out AccessibilitySettingsError error)
        {
            if (Current.TextScale == percent)
            {
                error = AccessibilitySettingsError.None;
                return true;
            }

            if (!Current.TrySetTextScale(percent, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TrySetHighContrastEnabled(bool enabled, out AccessibilitySettingsError error)
        {
            if (Current.HighContrastEnabled == enabled)
            {
                error = AccessibilitySettingsError.None;
                return true;
            }

            if (!Current.TrySetHighContrastEnabled(enabled, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }


        private AccessibilitySettingsState original;
        public bool HasChanges => original != null && !Same(Current, original);
        private static AccessibilitySettingsState Copy(AccessibilitySettingsState s) => new AccessibilitySettingsState(s.UiScale, s.TextScale, s.HighContrastEnabled);
        private static bool Same(AccessibilitySettingsState a, AccessibilitySettingsState b) => a.UiScale == b.UiScale && a.TextScale == b.TextScale && a.HighContrastEnabled == b.HighContrastEnabled;
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

            Current = new AccessibilitySettingsState();
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
            AccessibilitySettingsOutput.Publish(Current);
        }
    }
}
