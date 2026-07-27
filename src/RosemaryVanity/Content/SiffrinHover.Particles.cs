using Daybreak.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rosemary.Core;
using System.Collections.Generic;
using Rosemary.Common;
using Terraria;
using Terraria.UI;

namespace Rosemary.Vanity.Content;

public static partial class SiffrinParticles
{
    public record struct Star(Vector2 Position, Color Color, byte Style, byte Frame, int FrameCounter) : IUpdatingParticle
    {
        bool IUpdatingParticle.Update()
        {
            FrameCounter++;

            if (FrameCounter >= 4)
            {
                Frame++;
                FrameCounter = 0;
            }

            return Frame <= 5;
        }
    }

    public static UpdatingParticleHandler<Star> BackgroundStars { get; set; } = new(128);

    public static UpdatingParticleHandler<Star> ForegroundStars { get; set; } = new(256);

    public static UpdatingParticleHandler<Star> UIStars { get; set; } = new(256);

    [ModSystemHooks.PostUpdateDusts]
    private static void UpdateParticlesPostDust()
    {
        BackgroundStars.Update();
        ForegroundStars.Update();
    }

    [ModSystemHooks.UpdateUI]
    private static void UpdateParticlesUI(GameTime gameTime)
    {
        UIStars.Update();
    }

    [ParticleLayers.UnderPlayers]
    private static void DrawParticlesUnderPlayers(SpriteBatch sb)
    {
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        {
            DrawStars(sb, BackgroundStars, Main.screenPosition);
        }
        sb.End();
    }

    [ParticleLayers.OverPlayers]
    private static void DrawParticlesOverPlayers(SpriteBatch sb)
    {
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        {
            DrawStars(sb, ForegroundStars, Main.screenPosition);
        }
        sb.End();
    }

    [GameInterfaceLayers.After(GameInterfaceLayers.MOUSE_ITEM_NPC_HEAD, InterfaceScaleType.UI, Name = $"{nameof(RosemaryVanity)}: Siffrin UI Particles")]
    private static bool DrawParticlesUI()
    {
        DrawStars(Main.spriteBatch, UIStars, Vector2.Zero);

        return true;
    }

    private static void DrawStars(SpriteBatch sb, ParticleHandler<Star> stars, Vector2 offset)
    {
        if (stars.ActiveParticleCount <= 0)
        {
            return;
        }

        var texture = Assets.Vanity.Star.Asset.Value;

        var origin = new Vector2(9f);

        foreach (var index in stars)
        {
            var star = stars[index];

            var frame = texture.Frame(6, 4, star.Frame, star.Style);

            sb.Draw(texture, star.Position - offset, frame, star.Color, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}
