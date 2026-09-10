using Game.Client.Common;
using NUnit.Framework;

namespace Game.Architecture.Tests
{
    /// <summary>
    /// What the cover remembers between the view it is drawn on being
    /// destroyed and the next one arriving.
    /// </summary>
    /// <remarks>
    /// A scene load takes the view with it, so the overlay outlives its view
    /// and catches the next one up. Catching it up with an instruction that was
    /// withdrawn is how a cover ends up over a screen nobody is loading.
    /// </remarks>
    public sealed class LoadingOverlayStateTests
    {
        private sealed class SpyView : ILoadingView
        {
            public int Shows { get; private set; }

            public bool IsPresented { get; private set; }

            public void Show()
            {
                Shows++;
                IsPresented = true;
            }

            public void Hide() => IsPresented = false;

            public void HideImmediate() => IsPresented = false;
        }

        [Test]
        public void AShowWithNoView_IsAppliedToTheNextOne()
        {
            var overlay = new LoadingOverlay();
            overlay.Show();

            var view = new SpyView();
            overlay.Attach(view);

            Assert.That(view.IsPresented, Is.True);
        }

        /// <summary>
        /// The infinite loading: leaving a game hides the cover, the scene that
        /// held the view is replaced, and the cover comes back up on the new
        /// one with nothing left to wait for.
        /// </summary>
        [Test]
        public void AHiddenCover_DoesNotComeBackOnTheNextView()
        {
            var overlay = new LoadingOverlay();
            var first = new SpyView();
            overlay.Attach(first);

            overlay.Show();
            overlay.Hide();

            var second = new SpyView();
            overlay.Attach(second);

            Assert.That(second.IsPresented, Is.False);
            Assert.That(second.Shows, Is.Zero, "Nothing asked for it.");
        }

        [Test]
        public void HidingWithNoView_IsAlsoForgotten()
        {
            var overlay = new LoadingOverlay();
            overlay.Show();
            overlay.Hide();

            var view = new SpyView();
            overlay.Attach(view);

            Assert.That(view.IsPresented, Is.False);
        }

        [Test]
        public void ShowingAgainAfterAHide_StillReachesTheNextView()
        {
            var overlay = new LoadingOverlay();
            overlay.Show();
            overlay.Hide();
            overlay.Show();

            var view = new SpyView();
            overlay.Attach(view);

            Assert.That(view.IsPresented, Is.True);
        }
    }
}
