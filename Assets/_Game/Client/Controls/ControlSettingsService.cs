using System;
using Game.Core.Settings;

namespace Game.Client.Controls
{
    public sealed class ControlSettingsService : IControlSettings, Game.Client.Settings.ISettingsEdit
    {
        private readonly IControlSettingsStore store;
        private readonly IControlSettingsApplier applier;
        private bool rebinding;

        public ControlSettingsService(
            IControlSettingsStore store,
            IControlSettingsApplier applier)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.applier = applier ?? throw new ArgumentNullException(nameof(applier));
            Current = store.LoadOrDefault() ?? new ControlSettingsState();
            ApplyCurrent();
        }

        public ControlSettingsState Current { get; private set; }

        public ControlAction? ListeningAction { get; private set; }

        public event Action<ControlSettingsState> Changed;

        public event Action<ControlAction?> RebindListeningChanged;

        public event Action<ControlAction> BindingConflict;

        public bool TrySetPath(ControlAction action, string path, out ControlSettingsError error)
        {
            if (Current.GetPath(action) == path)
            {
                error = ControlSettingsError.None;
                return true;
            }

            if (!Current.TrySetPath(action, path, out error))
            {
                if (error == ControlSettingsError.DuplicatePath &&
                    Current.TryFindConflict(action, path, out var occupiedBy))
                {
                    BindingConflict?.Invoke(occupiedBy);
                }

                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TryStartRebind(ControlAction action, out ControlSettingsError error)
        {
            if (!Enum.IsDefined(typeof(ControlAction), action))
            {
                error = ControlSettingsError.UnknownAction;
                return false;
            }

            CancelRebind();
            rebinding = true;
            ListeningAction = action;
            RebindListeningChanged?.Invoke(action);
            applier.StartRebind(action, OnRebindCompleted, OnRebindCancelled);
            error = ControlSettingsError.None;
            return true;
        }

        public void CancelRebind()
        {
            if (!rebinding)
            {
                return;
            }

            rebinding = false;
            applier.CancelRebind();
            ClearListening();
        }

        private void OnRebindCompleted(string path)
        {
            var action = ListeningAction;
            rebinding = false;
            ClearListening();
            if (!action.HasValue)
            {
                return;
            }

            if (!TrySetPath(action.Value, path, out _))
            {
                ApplyCurrent();
            }
        }

        private void OnRebindCancelled()
        {
            if (!rebinding)
            {
                return;
            }

            rebinding = false;
            ClearListening();
        }

        private void ClearListening()
        {
            ListeningAction = null;
            RebindListeningChanged?.Invoke(null);
        }


        private ControlSettingsState original;
        public bool HasChanges => original != null && !Same(Current, original);
        private static ControlSettingsState Copy(ControlSettingsState s) => new ControlSettingsState(s);
        private static bool Same(ControlSettingsState a, ControlSettingsState b) => System.Linq.Enumerable.All(ControlSettingsState.Rows, row => a.GetPath(row.Action) == b.GetPath(row.Action));
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
            CancelRebind();
            Current = new ControlSettingsState();
            ApplyCurrent();
            Changed?.Invoke(Current);
        }
        public void CancelEdit()
        {
            CancelRebind();
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
        }
    }
}
