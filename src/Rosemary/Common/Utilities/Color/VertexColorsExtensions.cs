using Terraria.Graphics;

namespace Rosemary.Common;

public static class VertexColorsExtensions
{
    extension(ref VertexColors colors)
    {
        public void operator *= (float opacity)
        {
            colors.TopLeftColor *= opacity;
            colors.TopRightColor *= opacity;
            colors.BottomLeftColor *= opacity;
            colors.BottomRightColor *= opacity;
        }
    }
}
