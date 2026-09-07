using System.Collections.Generic;
using Game.Client.Accessibility;
using Game.Core.Settings;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class AccessibilitySettingsServiceTests
    {
        [Test]
        public void Construction_LoadsStoreAndAppliesImmediately()
        {
            var stored = new AccessibilitySettingsState(80, 20, true);
            var store = new MemoryAccessibilitySettingsStore { NextLoad = stored };
            var applier = new RecordingAccessibilitySettingsApplier();

            var service = new AccessibilitySettingsService(store, applier);

            Assert.That(service.Current.UiScale, Is.EqualTo(80));
            Assert.That(service.Current.TextScale, Is.EqualTo(20));
            Assert.That(service.Current.HighContrastEnabled, Is.True);
            Assert.That(applier.Applied.Count, Is.EqualTo(1));
            Assert.That(AccessibilitySettingsOutput.Current, Is.SameAs(service.Current));
        }

        [Test]
        public void TrySetUiScale_PersistsAndReapplies()
        {
            var store = new MemoryAccessibilitySettingsStore();
            var applier = new RecordingAccessibilitySettingsApplier();
            var service = new AccessibilitySettingsService(store, applier);
            var changedCount = 0;
            service.Changed += _ => changedCount++;

            Assert.That(service.TrySetUiScale(90, out var error), Is.True);
            Assert.That(error, Is.EqualTo(AccessibilitySettingsError.None));
            Assert.That(service.Current.UiScale, Is.EqualTo(90));
            Assert.That(store.Saved.UiScale, Is.EqualTo(90));
            Assert.That(applier.Applied.Count, Is.EqualTo(2));
            Assert.That(changedCount, Is.EqualTo(1));

            Assert.That(service.TrySetUiScale(90, out error), Is.True);
            Assert.That(applier.Applied.Count, Is.EqualTo(2));
            Assert.That(changedCount, Is.EqualTo(1));
        }

        [Test]
        public void TrySetHighContrast_PersistsWithoutChangingScales()
        {
            var store = new MemoryAccessibilitySettingsStore();
            var applier = new RecordingAccessibilitySettingsApplier();
            var service = new AccessibilitySettingsService(store, applier);

            Assert.That(service.TrySetTextScale(70, out _), Is.True);
            Assert.That(service.TrySetHighContrastEnabled(true, out _), Is.True);
            Assert.That(service.Current.HighContrastEnabled, Is.True);
            Assert.That(service.Current.TextScale, Is.EqualTo(70));
            Assert.That(store.Saved.HighContrastEnabled, Is.True);
        }


        [Test]
        public void Editing_PreviewsWithoutSaving_ApplyAndCancelKeepCommittedValues()
        {
            var store = new MemoryAccessibilitySettingsStore();
            var service = new AccessibilitySettingsService(store, new RecordingAccessibilitySettingsApplier());
            service.BeginEdit();
            service.TrySetUiScale(17, out _);
            Assert.That(service.HasChanges, Is.True);
            Assert.That(store.Saved, Is.Null);
            service.CancelEdit();
            Assert.That(service.Current.UiScale, Is.EqualTo(AccessibilitySettingsState.DefaultScale));
            service.BeginEdit();
            service.TrySetUiScale(17, out _);
            service.ApplyEdit();
            Assert.That(store.Saved.UiScale, Is.EqualTo(17));
            Assert.That(service.HasChanges, Is.False);
            service.ResetEdit();
            Assert.That(service.HasChanges, Is.True);
            Assert.That(store.Saved.UiScale, Is.EqualTo(17));
            service.CancelEdit();
            Assert.That(service.Current.UiScale, Is.EqualTo(17));
            store.NextLoad = store.Saved;
            var reloaded = new AccessibilitySettingsService(store, new RecordingAccessibilitySettingsApplier());
            Assert.That(reloaded.Current.UiScale, Is.EqualTo(17));
        }

        private sealed class MemoryAccessibilitySettingsStore : IAccessibilitySettingsStore
        {
            public AccessibilitySettingsState NextLoad { get; set; }

            public AccessibilitySettingsState Saved { get; private set; }

            public AccessibilitySettingsState LoadOrDefault()
            {
                return NextLoad ?? new AccessibilitySettingsState();
            }

            public void Save(AccessibilitySettingsState settings)
            {
                Saved = settings;
            }
        }

        private sealed class RecordingAccessibilitySettingsApplier : IAccessibilitySettingsApplier
        {
            public List<AccessibilitySettingsState> Applied { get; } =
                new List<AccessibilitySettingsState>();

            public void Apply(AccessibilitySettingsState settings)
            {
                Applied.Add(settings);
            }
        }
    }
}
