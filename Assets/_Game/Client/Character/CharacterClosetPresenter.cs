using System;
using Game.Client.Home;
using Game.Core.Players;
using UnityEngine;
using VContainer.Unity;

namespace Game.Client.Character
{
    /// <summary>
    /// Keeps the closet's draft: what the player has picked but not yet
    /// applied.
    /// </summary>
    /// <remarks>
    /// The draft lives here and nowhere else. The character in the middle is
    /// dressed in it, the grid marks it, and
    /// <see cref="AvatarAppearanceState"/> is left holding what was last
    /// applied — so leaving without applying is nothing more than this object
    /// going away.
    /// <para>
    /// Applied and draft are held side by side rather than as a dirty flag,
    /// because reset has to put the applied one back and the buttons have to
    /// go dark again when a player undoes their own change by hand.
    /// </para>
    /// </remarks>
    public sealed class CharacterClosetPresenter : IStartable, IDisposable
    {
        private readonly ICharacterClosetView view;
        private readonly AvatarPartCatalog catalog;
        private readonly AvatarAppearanceState appearance;
        private readonly IHomeApplicationHost applicationHost;

        private AvatarAppearance draft;
        private AvatarAppearance applied;
        private AvatarPartCategory shownCategory;
        private bool hasShownCategory;
        private ClosetConfirmKind? pending;

        public CharacterClosetPresenter(
            ICharacterClosetView view,
            AvatarPartCatalog catalog,
            AvatarAppearanceState appearance,
            IHomeApplicationHost applicationHost)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
            this.applicationHost = applicationHost
                                   ?? throw new ArgumentNullException(nameof(applicationHost));
        }

        /// <summary>What has been picked but not applied. For tests.</summary>
        public AvatarAppearance Draft => draft;

        public void Start()
        {
            view.BackRequested += OnBackRequested;
            view.CategorySelected += OnCategorySelected;
            view.PartSelected += OnPartSelected;
            view.ResetRequested += OnResetRequested;
            view.ApplyRequested += OnApplyRequested;
            view.ConfirmAccepted += OnConfirmAccepted;
            view.ConfirmDismissed += OnConfirmDismissed;

            applied = Worn();
            draft = applied;
            view.ShowCategories(catalog.Groups);
            view.ShowPreview(draft);
            view.HideConfirm();
            view.SetActionsEnabled(false);

            if (catalog.Groups.Count > 0 && catalog.Groups[0] != null)
            {
                ShowCategory(catalog.Groups[0].Category);
            }
            else
            {
                Debug.LogWarning(
                    "[Closet] The part catalogue is empty; there is nothing to pick.");
            }
        }

        public void Dispose()
        {
            view.BackRequested -= OnBackRequested;
            view.CategorySelected -= OnCategorySelected;
            view.PartSelected -= OnPartSelected;
            view.ResetRequested -= OnResetRequested;
            view.ApplyRequested -= OnApplyRequested;
            view.ConfirmAccepted -= OnConfirmAccepted;
            view.ConfirmDismissed -= OnConfirmDismissed;
        }

        /// <summary>Whether there is anything to apply or to undo.</summary>
        private bool IsChanged => draft != applied;

        /// <summary>
        /// What the screen opens on: the applied appearance, or the
        /// catalogue's default for a player who has never applied one.
        /// </summary>
        /// <remarks>
        /// Run through the catalogue either way, so an appearance saved before
        /// an art change opens as something that can actually be drawn.
        /// </remarks>
        private AvatarAppearance Worn()
        {
            var current = appearance.Current;
            return catalog.Normalise(
                current == AvatarAppearance.Default ? catalog.Default : current);
        }

        private void OnCategorySelected(AvatarPartCategory category)
        {
            if (hasShownCategory && category == shownCategory)
            {
                return;
            }

            ShowCategory(category);
        }

        /// <summary>
        /// Wears the part at once. Nothing is saved: the draft is what the
        /// character in the middle is wearing, and only apply moves it into
        /// <see cref="AvatarAppearanceState"/>.
        /// </summary>
        private void OnPartSelected(AvatarPartCategory category, string partId)
        {
            // A cell from a category that is no longer on screen. Cannot happen
            // through the grid, which is rebuilt on every switch, but the event
            // carries the category precisely so it need not be trusted.
            if (!hasShownCategory || category != shownCategory)
            {
                return;
            }

            // Every category wears something, so an empty id is not a cell
            // the grid can draw — only a call that got here another way.
            if (!catalog.TryFind(category, partId, out _))
            {
                return;
            }

            var next = draft.With(category, partId);
            if (next == draft)
            {
                return;
            }

            draft = next;
            view.ShowSelectedPart(category, partId);
            view.ShowPreview(draft);
            view.SetActionsEnabled(IsChanged);
        }

        /// <summary>
        /// Settles on the draft. Nothing is asked first: applying is what the
        /// player came to do, and the design only confirms the two ways of
        /// throwing work away.
        /// </summary>
        private void OnApplyRequested()
        {
            if (!IsChanged)
            {
                return;
            }

            appearance.Apply(draft);
            applied = draft;
            view.SetActionsEnabled(false);
        }

        private void OnResetRequested()
        {
            if (!IsChanged)
            {
                return;
            }

            Ask(ClosetConfirmKind.Reset);
        }

        /// <summary>
        /// Leaves, or asks first if there is something to lose.
        /// </summary>
        private void OnBackRequested()
        {
            if (pending.HasValue)
            {
                return;
            }

            if (!IsChanged)
            {
                applicationHost.OpenHome();
                return;
            }

            Ask(ClosetConfirmKind.Discard);
        }

        private void OnConfirmAccepted()
        {
            if (!pending.HasValue)
            {
                return;
            }

            var kind = pending.Value;
            pending = null;
            view.HideConfirm();

            if (kind == ClosetConfirmKind.Reset)
            {
                Restore();
                return;
            }

            applicationHost.OpenHome();
        }

        private void OnConfirmDismissed()
        {
            if (!pending.HasValue)
            {
                return;
            }

            pending = null;
            view.HideConfirm();
        }

        private void Ask(ClosetConfirmKind kind)
        {
            pending = kind;
            view.ShowConfirm(kind);
        }

        /// <summary>
        /// Back to the last applied appearance — not to a factory default. The
        /// panel says the changes disappear, and what is left when they do is
        /// what the player was already wearing.
        /// </summary>
        private void Restore()
        {
            draft = applied;
            if (hasShownCategory)
            {
                view.ShowSelectedPart(shownCategory, draft.Get(shownCategory));
            }

            view.ShowPreview(draft);
            view.SetActionsEnabled(false);
        }

        private void ShowCategory(AvatarPartCategory category)
        {
            var group = catalog.Find(category);
            if (group == null)
            {
                return;
            }

            shownCategory = category;
            hasShownCategory = true;
            view.ShowParts(group, draft.Get(category));
        }
    }
}
