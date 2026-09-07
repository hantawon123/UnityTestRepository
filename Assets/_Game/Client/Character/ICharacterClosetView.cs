using System;
using System.Collections.Generic;
using Game.Core.Players;

namespace Game.Client.Character
{
    /// <summary>
    /// The closet screen, as the presenter needs it: four things it can be
    /// asked to draw and three things the player can do on it.
    /// </summary>
    public interface ICharacterClosetView
    {
        /// <summary>The arrow at the top left.</summary>
        event Action BackRequested;

        event Action<AvatarPartCategory> CategorySelected;

        /// <summary>
        /// A cell in the grid, carrying the part it stands for. An empty id is
        /// the first cell, which takes the part off.
        /// </summary>
        event Action<AvatarPartCategory, string> PartSelected;

        /// <summary>Fills the rail down the left. Called once.</summary>
        void ShowCategories(IReadOnlyList<AvatarPartGroup> groups);

        /// <summary>
        /// Puts a category's parts in the locker and marks its tab as the one
        /// being looked at.
        /// </summary>
        void ShowParts(AvatarPartGroup group, string selectedPartId);

        /// <summary>
        /// Moves the selection ring without rebuilding the grid, for the common
        /// case of picking within the category already shown.
        /// </summary>
        void ShowSelectedPart(AvatarPartCategory category, string partId);

        /// <summary>Dresses the character standing in the middle.</summary>
        void ShowPreview(AvatarAppearance appearance);
    }
}
