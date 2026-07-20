using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Rosemary.Common;

public static class BlendStateExtensions
{
    private static readonly BlendState multiplicative = new BlendState
    {
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        ColorDestinationBlend = Blend.One,
        ColorSourceBlend = Blend.SourceAlpha,
        AlphaBlendFunction = BlendFunction.ReverseSubtract,
        AlphaDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.SourceAlpha,
    };

    extension(BlendState)
    {
        public static BlendState Multiplicative => multiplicative;
    }
}
