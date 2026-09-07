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
    /// Applying, resetting and the two confirmations are the next story. Until
    /// then the arrow leaves at once, which is also the behaviour the design
    /// asks for when nothing has been changed.
    /// </para>
    /// </remarks>
    public sealed class CharacterClosetPresenter : IStartable, IDisposable
    {
        private readonly ICharacterClosetView view;
        private readonly AvatarPartCatalog catalog;
        private readonly AvatarAppearanceState appearance;
        private readonly IHomeApplicationHost applicationHost;

        private AvatarAppearance draft;
        private AvatarPartCategory shownCategory;
        private bool hasShownCategory;

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

            draft = Worn();
            view.ShowCategories(catalog.Groups);
            view.ShowPreview(draft);

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
        }

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
        }

        private void OnBackRequested()
        {
            applicationHost.OpenHome();
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
