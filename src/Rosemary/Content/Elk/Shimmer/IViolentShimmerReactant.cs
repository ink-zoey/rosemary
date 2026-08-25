using Terraria;

namespace Rosemary.Content.Elk;

/// <summary>
///     Only applicable for use with <c>ItemID.Sets.ViolentShimmerReaction</c>.
/// </summary>
public interface IViolentShimmerReactant
{
    /// <summary>
    ///     Ran when the item is ejected from the shimmer surface.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> if the item should be removed.
    /// </returns>
    bool Ejection(WorldItem item, bool subSurface);
}
