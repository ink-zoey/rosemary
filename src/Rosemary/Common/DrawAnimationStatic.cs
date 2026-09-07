using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;

namespace Rosemary.Common;

public class DrawAnimationStatic(
    int horizontalFrames = 1,
    int verticalFrames = 1,
    int frameX = 0,
    int frameY = 0,
    int sizeOffsetX = 0,
    int sizeOffsetY = 0
) : DrawAnimation
{
    public override Rectangle GetFrame(Texture2D texture, int frameCounterOverride = -1)
    {
        return texture.Frame(horizontalFrames, verticalFrames, frameX, frameY, sizeOffsetX, sizeOffsetY);
    }
}
