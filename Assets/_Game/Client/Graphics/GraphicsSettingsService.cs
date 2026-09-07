using System;
using Game.Core.Settings;

namespace Game.Client.Graphics
{
    public sealed class GraphicsSettingsService : IGraphicsSettings, Game.Client.Settings.ISettingsEdit
    {
        private readonly IGraphicsSettingsStore store;
        private readonly IGraphicsSettingsApplier applier;

        public GraphicsSettingsService(
            IGraphicsSettingsStore store,
            IGraphicsSettingsApplier applier)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.applier = applier ?? throw new ArgumentNullException(nameof(applier));
            Current = store.LoadOrDefault() ?? new GraphicsSettingsState();
            ApplyCurrent();
        }

        public GraphicsSettingsState Current { get; private set; }

        public event Action<GraphicsSettingsState> Changed;

        public bool TrySetQuality(GraphicsQualityPreset quality, out GraphicsSettingsError error)
        {
            if (Current.Quality == quality)
            {
                error = GraphicsSettingsError.None;
                return true;
            }

            if (!Current.TrySetQuality(quality, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TrySetResolution(int index, out GraphicsSettingsError error)
        {
            if (Current.ResolutionIndex == index)
            {
                error = GraphicsSettingsError.None;
                return true;
            }

            if (!Current.TrySetResolution(index, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TrySetDisplayMode(DisplayMode mode, out GraphicsSettingsError error)
        {
            if (Current.DisplayMode == mode)
            {
                error = GraphicsSettingsError.None;
                return true;
            }

            if (!Current.TrySetDisplayMode(mode, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TrySetFrameCap(int index, out GraphicsSettingsError error)
        {
            if (Current.FrameCapIndex == index)
            {
                error = GraphicsSettingsError.None;
                return true;
            }

            if (!Current.TrySetFrameCap(index, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TrySetShadows(ShadowQualityLevel shadows, out GraphicsSettingsError error)
        {
            if (Current.Shadows == shadows)
            {
                error = GraphicsSettingsError.None;
                return true;
            }

            if (!Current.TrySetShadows(shadows, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TrySetEffects(EffectsQualityLevel effects, out GraphicsSettingsError error)
        {
            if (Current.Effects == effects)
            {
                error = GraphicsSettingsError.None;
                return true;
            }

            if (!Current.TrySetEffects(effects, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TrySetAntiAliasing(AntiAliasingMode antiAliasing, out GraphicsSettingsError error)
        {
            if (Current.AntiAliasing == antiAliasing)
            {
                error = GraphicsSettingsError.None;
                return true;
            }

            if (!Current.TrySetAntiAliasing(antiAliasing, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }

        public bool TrySetBrightness(int percent, out GraphicsSettingsError error)
        {
            if (Current.Brightness == percent)
            {
                error = GraphicsSettingsError.None;
                return true;
            }

            if (!Current.TrySetBrightness(percent, out error))
            {
                return false;
            }

            PersistAndApply();
            return true;
        }


        private GraphicsSettingsState original;
        public bool HasChanges => original != null && !Same(Current, original);
        private static GraphicsSettingsState Copy(GraphicsSettingsState s) => new GraphicsSettingsState(s.Quality, s.ResolutionIndex, s.DisplayMode, s.FrameCapIndex, s.Shadows, s.Effects, s.AntiAliasing, s.Brightness);
        private static bool Same(GraphicsSettingsState a, GraphicsSettingsState b) => a.Quality == b.Quality && a.ResolutionIndex == b.ResolutionIndex && a.DisplayMode == b.DisplayMode && a.FrameCapIndex == b.FrameCapIndex && a.Shadows == b.Shadows && a.Effects == b.Effects && a.AntiAliasing == b.AntiAliasing && a.Brightness == b.Brightness;
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

            Current = new GraphicsSettingsState();
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
            GraphicsSettingsOutput.Publish(Current);
        }
    }
}
