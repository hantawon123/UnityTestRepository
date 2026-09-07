using System;
using System.Collections.Generic;
using Game.Client.Character;
using Game.Client.Home;
using Game.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace Game.Architecture.Tests
{
    /// <summary>
    /// What the closet shows when it opens, and what picking a part does.
    /// </summary>
    /// <remarks>
    /// The catalogue is built through <see cref="JsonUtility"/> rather than by
    /// hand: its parts are serialised fields with no public setters, and a test
    /// hook for them would be a hole in the asset's own contract. The strings
    /// below are the same shape the .asset file has.
    /// </remarks>
    public sealed class CharacterClosetPresenterTests
    {
        private const string TwoCategories =
            "{\"groups\":[" +
            "{\"category\":0,\"label\":\"몸 색상\",\"parts\":[" +
            "{\"id\":\"body_a\",\"label\":\"A\"},{\"id\":\"body_b\",\"label\":\"B\"}]}," +
            "{\"category\":1,\"label\":\"후드\",\"parts\":[" +
            "{\"id\":\"hood_a\",\"label\":\"A\"},{\"id\":\"hood_b\",\"label\":\"B\"}]}]}";

        private AvatarPartCatalog catalog;
        private FakeClosetView view;
        private FakeApplicationHost host;

        [SetUp]
        public void SetUp()
        {
            catalog = ScriptableObject.CreateInstance<AvatarPartCatalog>();
            JsonUtility.FromJsonOverwrite(TwoCategories, catalog);
            view = new FakeClosetView();
            host = new FakeApplicationHost();
        }

        [TearDown]
        public void TearDown()
        {
            if (catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void Opening_ShowsEveryCategory_AndTheFirstOnesParts()
        {
            var presenter = Presenter(new AvatarAppearanceState());
            presenter.Start();

            Assert.That(view.ShownCategories, Is.EqualTo(2));
            Assert.That(view.ShownGroup, Is.EqualTo(AvatarPartCategory.BodyColor));
            presenter.Dispose();
        }

        [Test]
        public void Opening_WithNothingApplied_WearsTheCatalogueDefault()
        {
            var presenter = Presenter(new AvatarAppearanceState());
            presenter.Start();

            // The first part of every category the catalogue describes.
            Assert.That(presenter.Draft.BodyColorId, Is.EqualTo("body_a"));
            Assert.That(presenter.Draft.HoodId, Is.EqualTo("hood_a"));
            Assert.That(view.Previewed, Is.EqualTo(presenter.Draft));
            presenter.Dispose();
        }

        [Test]
        public void Opening_WearsWhatWasApplied()
        {
            var applied = new AvatarAppearance("body_b", "hood_a", string.Empty, string.Empty);
            var presenter = Presenter(Wearing(applied));
            presenter.Start();

            Assert.That(presenter.Draft, Is.EqualTo(applied));
            Assert.That(view.SelectedPartId, Is.EqualTo("body_b"));
            presenter.Dispose();
        }

        [Test]
        public void Opening_DropsAPartTheCatalogueNoLongerHas()
        {
            var applied = new AvatarAppearance(
                "body_gone", "hood_gone", string.Empty, string.Empty);
            var presenter = Presenter(Wearing(applied));
            presenter.Start();

            // Both fall back to their category's first part: nothing on this
            // character is optional.
            Assert.That(presenter.Draft.BodyColorId, Is.EqualTo("body_a"));
            Assert.That(presenter.Draft.HoodId, Is.EqualTo("hood_a"));
            presenter.Dispose();
        }

        [Test]
        public void PickingAPart_WearsItAtOnce_WithoutApplyingIt()
        {
            var appearance = new AvatarAppearanceState();
            var presenter = Presenter(appearance);
            presenter.Start();

            view.PickPart(AvatarPartCategory.BodyColor, "body_b");

            Assert.That(presenter.Draft.BodyColorId, Is.EqualTo("body_b"));
            Assert.That(view.Previewed.BodyColorId, Is.EqualTo("body_b"));
            Assert.That(view.SelectedPartId, Is.EqualTo("body_b"));
            Assert.That(
                appearance.Current,
                Is.EqualTo(AvatarAppearance.Default),
                "Nothing is settled until apply, which is the next story.");
            presenter.Dispose();
        }

        [Test]
        public void SwitchingCategory_ShowsItsParts_AndKeepsWhatWasPicked()
        {
            var presenter = Presenter(new AvatarAppearanceState());
            presenter.Start();
            view.PickPart(AvatarPartCategory.BodyColor, "body_b");

            view.PickCategory(AvatarPartCategory.Hood);

            Assert.That(view.ShownGroup, Is.EqualTo(AvatarPartCategory.Hood));
            Assert.That(presenter.Draft.BodyColorId, Is.EqualTo("body_b"));
            presenter.Dispose();
        }

        /// <summary>
        /// There is no empty cell in the grid, so nothing can ask to wear
        /// nothing. A call that does is a caller in the wrong, not a choice.
        /// </summary>
        [Test]
        public void PickingNothing_LeavesTheCharacterDressed()
        {
            var presenter = Presenter(new AvatarAppearanceState());
            presenter.Start();

            view.PickPart(AvatarPartCategory.BodyColor, AvatarAppearance.NoPart);

            Assert.That(presenter.Draft.BodyColorId, Is.EqualTo("body_a"));
            presenter.Dispose();
        }

        [Test]
        public void PickingAPartTheCatalogueDoesNotHave_IsIgnored()
        {
            var presenter = Presenter(new AvatarAppearanceState());
            presenter.Start();

            view.PickPart(AvatarPartCategory.BodyColor, "body_invented");

            Assert.That(presenter.Draft.BodyColorId, Is.EqualTo("body_a"));
            presenter.Dispose();
        }

        [Test]
        public void PickingAPartFromACategoryNotOnScreen_IsIgnored()
        {
            var presenter = Presenter(new AvatarAppearanceState());
            presenter.Start();

            // The body colours are the ones on screen.
            view.PickPart(AvatarPartCategory.Hood, "hood_b");

            Assert.That(presenter.Draft.HoodId, Is.EqualTo("hood_a"));
            presenter.Dispose();
        }

        [Test]
        public void TheArrow_LeavesForHome()
        {
            var presenter = Presenter(new AvatarAppearanceState());
            presenter.Start();

            view.PressBack();

            Assert.That(host.HomeOpenCount, Is.EqualTo(1));
            presenter.Dispose();
        }

        [Test]
        public void Disposing_StopsListeningToTheScreen()
        {
            var presenter = Presenter(new AvatarAppearanceState());
            presenter.Start();
            presenter.Dispose();

            view.PressBack();

            Assert.That(host.HomeOpenCount, Is.Zero);
        }

        private CharacterClosetPresenter Presenter(AvatarAppearanceState appearance) =>
            new CharacterClosetPresenter(view, catalog, appearance, host);

        /// <summary>A player who has applied something already.</summary>
        private static AvatarAppearanceState Wearing(AvatarAppearance appearance)
        {
            var state = new AvatarAppearanceState();
            state.Apply(appearance);
            return state;
        }

        private sealed class FakeClosetView : ICharacterClosetView
        {
            public event Action BackRequested;

            public event Action<AvatarPartCategory> CategorySelected;

            public event Action<AvatarPartCategory, string> PartSelected;

            public int ShownCategories { get; private set; }

            public AvatarPartCategory? ShownGroup { get; private set; }

            public string SelectedPartId { get; private set; }

            public AvatarAppearance Previewed { get; private set; }

            public void ShowCategories(IReadOnlyList<AvatarPartGroup> groups)
            {
                ShownCategories = groups?.Count ?? 0;
            }

            public void ShowParts(AvatarPartGroup group, string selectedPartId)
            {
                ShownGroup = group.Category;
                SelectedPartId = selectedPartId;
            }

            public void ShowSelectedPart(AvatarPartCategory category, string partId)
            {
                SelectedPartId = partId;
            }

            public void ShowPreview(AvatarAppearance appearance)
            {
                Previewed = appearance;
            }

            public void PressBack() => BackRequested?.Invoke();

            public void PickCategory(AvatarPartCategory category) =>
                CategorySelected?.Invoke(category);

            public void PickPart(AvatarPartCategory category, string partId) =>
                PartSelected?.Invoke(category, partId);
        }

        private sealed class FakeApplicationHost : IHomeApplicationHost
        {
            public int HomeOpenCount { get; private set; }

            public void Quit()
            {
            }

            public void OpenHome() => HomeOpenCount++;

            public void OpenRoomBrowser()
            {
            }

            public void OpenCharacterCloset()
            {
            }

            public void CreateRoom(string title, bool isPublic, int maxPlayers)
            {
            }

            public void OpenLobby()
            {
            }
        }
    }
}
