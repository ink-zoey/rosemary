using Microsoft.Xna.Framework.Graphics;

namespace Rosemary.Common;

public static class BlendStateExtensions
{
    private static readonly BlendState inverse_multiplicative = new BlendState
    {
        Name = $"{nameof(Rosemary)}.{nameof(inverse_multiplicative)}",
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        ColorDestinationBlend = Blend.One,
        ColorSourceBlend = Blend.SourceAlpha,
        AlphaBlendFunction = BlendFunction.ReverseSubtract,
        AlphaDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.SourceAlpha,
    };

    private static readonly BlendState alpha_mask = new BlendState
    {
        Name = $"{nameof(Rosemary)}.{nameof(alpha_mask)}",
        AlphaBlendFunction = BlendFunction.Add,
        AlphaDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.DestinationAlpha,
        ColorDestinationBlend = Blend.One,
        ColorSourceBlend = Blend.DestinationAlpha,
    };

    extension(BlendState)
    {
        public static BlendState InverseMultiplicative => inverse_multiplicative;

        public static BlendState AlphaMask => alpha_mask;
    }
}
