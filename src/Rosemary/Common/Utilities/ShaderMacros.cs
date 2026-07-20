// ReSharper disable RedundantUsingDirective.Global
global using static Rosemary.Common.ShaderMacros;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

// ReSharper disable UnusedMember.Global
namespace Rosemary.Common;

public static class ShaderMacros
{
    public static Vector2 TextureSize(int register)
    {
        if (Main.graphics.GraphicsDevice.Textures[register] is not Texture2D tex)
        {
            return Vector2.Zero;
        }

        return new Vector2(tex.Width, tex.Height);
    }

    public static Vector2 ViewportSize()
    {
        return Main.graphics.GraphicsDevice.Viewport.Bounds.Size();
    }
}
