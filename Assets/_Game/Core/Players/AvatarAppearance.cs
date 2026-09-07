using System;

namespace Game.Core.Players
{
    /// <summary>
    /// What a character is wearing: one part id per category, or none.
    /// </summary>
    /// <remarks>
    /// A value rather than a mutable object because the closet keeps two of
    /// these side by side — what was applied and what is being tried on — and
    /// compares them to decide whether anything changed. Sharing one instance
    /// between the two would make that comparison always agree.
    /// <para>
    /// Ids are the catalogue's, not the art's: nothing here knows what a
    /// texture or a mesh is, so the same value survives an art pass and can be
    /// stored by the account or sent over the wire.
    /// </para>
    /// </remarks>
    public readonly struct AvatarAppearance : IEquatable<AvatarAppearance>
    {
        /// <summary>
        /// Nothing worn in that category. Also what an unknown id becomes, so a
        /// part that leaves the catalogue leaves the character rather than
        /// leaving it half-dressed in something that no longer exists.
        /// </summary>
        public const string NoPart = "";

        /// <summary>
        /// Every category empty, which wears whatever the model was authored
        /// with. The catalogue, not this, decides what a new player starts in.
        /// </summary>
        public static readonly AvatarAppearance Default = default;

        public AvatarAppearance(
            string bodyColorId, string hoodId, string shoesId, string faceId)
        {
            BodyColorId = Normalise(bodyColorId);
            HoodId = Normalise(hoodId);
            ShoesId = Normalise(shoesId);
            FaceId = Normalise(faceId);
        }

        public string BodyColorId { get; }

        public string HoodId { get; }

        public string ShoesId { get; }

        public string FaceId { get; }

        public static bool operator ==(AvatarAppearance left, AvatarAppearance right) =>
            left.Equals(right);

        public static bool operator !=(AvatarAppearance left, AvatarAppearance right) =>
            !left.Equals(right);

        /// <summary>What is worn in one category.</summary>
        public string Get(AvatarPartCategory category)
        {
            switch (category)
            {
                case AvatarPartCategory.BodyColor:
                    return Normalise(BodyColorId);
                case AvatarPartCategory.Hood:
                    return Normalise(HoodId);
                case AvatarPartCategory.Shoes:
                    return Normalise(ShoesId);
                case AvatarPartCategory.Face:
                    return Normalise(FaceId);
                default:
                    throw new ArgumentOutOfRangeException(nameof(category));
            }
        }

        /// <summary>
        /// The same appearance with one category changed. Returns a new value;
        /// this one is left alone.
        /// </summary>
        public AvatarAppearance With(AvatarPartCategory category, string partId)
        {
            switch (category)
            {
                case AvatarPartCategory.BodyColor:
                    return new AvatarAppearance(partId, HoodId, ShoesId, FaceId);
                case AvatarPartCategory.Hood:
                    return new AvatarAppearance(BodyColorId, partId, ShoesId, FaceId);
                case AvatarPartCategory.Shoes:
                    return new AvatarAppearance(BodyColorId, HoodId, partId, FaceId);
                case AvatarPartCategory.Face:
                    return new AvatarAppearance(BodyColorId, HoodId, ShoesId, partId);
                default:
                    throw new ArgumentOutOfRangeException(nameof(category));
            }
        }

        public bool Equals(AvatarAppearance other) =>
            string.Equals(Get(AvatarPartCategory.BodyColor), other.Get(AvatarPartCategory.BodyColor), StringComparison.Ordinal) &&
            string.Equals(Get(AvatarPartCategory.Hood), other.Get(AvatarPartCategory.Hood), StringComparison.Ordinal) &&
            string.Equals(Get(AvatarPartCategory.Shoes), other.Get(AvatarPartCategory.Shoes), StringComparison.Ordinal) &&
            string.Equals(Get(AvatarPartCategory.Face), other.Get(AvatarPartCategory.Face), StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is AvatarAppearance other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + Get(AvatarPartCategory.BodyColor).GetHashCode();
                hash = (hash * 31) + Get(AvatarPartCategory.Hood).GetHashCode();
                hash = (hash * 31) + Get(AvatarPartCategory.Shoes).GetHashCode();
                hash = (hash * 31) + Get(AvatarPartCategory.Face).GetHashCode();
                return hash;
            }
        }

        public override string ToString() =>
            $"body={Describe(BodyColorId)}, hood={Describe(HoodId)}, " +
            $"shoes={Describe(ShoesId)}, face={Describe(FaceId)}";

        /// <summary>
        /// A default value carries nulls rather than empty strings, and a store
        /// that round-trips one may hand back either. Both mean the same thing,
        /// so they are made the same thing here rather than at every comparison.
        /// </summary>
        private static string Normalise(string partId) => partId ?? NoPart;

        private static string Describe(string partId) =>
            string.IsNullOrEmpty(partId) ? "none" : partId;
    }
}
