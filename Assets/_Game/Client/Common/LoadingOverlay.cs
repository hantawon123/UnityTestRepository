using Cysharp.Threading.Tasks;

namespace Game.Client.Common
{
    public interface ILoadingOverlay
    {
        bool IsPresented { get; }
        void Show();
        void Hide();
        void HideImmediate();
        void Attach(ILoadingView view);
    }

    /// <summary>
    /// Application-wide handle for the loading cover. The live view is attached
    /// once the project scope builds it; tests and early calls no-op until then.
    /// </summary>
    public sealed class LoadingOverlay : ILoadingOverlay
    {
        private ILoadingView view;
        private bool shown;

        public bool IsPresented => view != null ? view.IsPresented : shown;

        public void Attach(ILoadingView view)
        {
            this.view = view;
            if (view == null)
            {
                return;
            }

            if (shown)
            {
                view.Show();
                return;
            }

            view.HideImmediate();
        }

        public void Show()
        {
            shown = true;
            view?.Show();
        }

        public void Hide()
        {
            if (view != null)
            {
                view.Hide();
                return;
            }

            shown = false;
        }

        public void HideImmediate()
        {
            shown = false;
            if (view != null)
            {
                view.HideImmediate();
            }
        }
    }

    public static class LoadingOverlayPaint
    {
        public static async UniTask ShowPainted(this ILoadingOverlay overlay)
        {
            overlay.Show();
            await SceneLoadSlicer.YieldFrame();
        }
    }
}
