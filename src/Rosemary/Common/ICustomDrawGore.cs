using Daybreak.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;

namespace Rosemary.Common;

public interface ICustomDrawGore
{
    bool PreDraw(Gore gore, SpriteBatch sb, DrawData drawData) => true;

    void PostDraw(Gore gore, SpriteBatch sb, DrawData drawData)
    { }
}

file static class CustomDrawGoreRenderer
{
    [OnLoad]
    private static void Load()
    {
        IL_Main.DrawGore += DrawGore_CustomDraw;
    }

    private static void DrawGore_CustomDraw(ILContext il)
    {
        var c = new ILCursor(il);

        var goreIndexIndex = -1;

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.gore)),
            i => i.MatchLdloc(out goreIndexIndex)
        );

        while (c.TryGotoNext(
            MoveType.Before,
            i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.Draw))
        ))
        {
            var jumpDrawTarget = c.DefineLabel();

            c.EmitLdloc(goreIndexIndex);
            c.EmitDelegate(
                static (
                    SpriteBatch sb,
                    Texture2D texture,
                    Vector2 position,
                    Rectangle? sourceRectangle,
                    Color color,
                    float rotation,
                    Vector2 origin,
                    float scale,
                    SpriteEffects effects,
                    float layerDepth,
                    int index
                ) =>
                {
                    var drawData = new DrawData(texture, position, sourceRectangle, color, rotation, origin, scale, effects, layerDepth);

                    var gore = Main.gore[index];

                    if (gore.ModGore is not ICustomDrawGore drawer)
                    {
                        drawData.Draw(sb);
                        return;
                    }

                    if (drawer.PreDraw(gore, sb, drawData))
                    {
                        drawData.Draw(sb);
                    }
                    drawer.PostDraw(gore, sb, drawData);
                }
            );

            c.EmitBr(jumpDrawTarget);

            c.GotoNext(
                MoveType.After,
                i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.Draw))
            );

            c.MarkLabel(jumpDrawTarget);
        }
    }
}
