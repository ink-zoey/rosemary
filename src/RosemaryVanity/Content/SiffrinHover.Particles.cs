using Daybreak.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rosemary.Common;
using Rosemary.Core;
using System;
using Terraria;
using Terraria.Graphics.Effects;
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

    public record struct TransformStar(Vector2 Position, float LifeTime) : IUpdatingParticle
    {
        bool IUpdatingParticle.Update()
        {
            LifeTime += 0.065f;

            return LifeTime <= 1f;
        }
    }

    public static UpdatingParticleHandler<Star> BackgroundStars { get; set; } = new(128);

    public static UpdatingParticleHandler<Star> ForegroundStars { get; set; } = new(128);

    public static UpdatingParticleHandler<TransformStar> TransformAnimation { get; set; } = new(32);

    public static UpdatingParticleHandler<Star> UIStars { get; set; } = new(128);

    [ModSystemHooks.PostUpdateDusts]
    private static void UpdateParticlesPostDust()
    {
        BackgroundStars.Update();
        ForegroundStars.Update();
        TransformAnimation.Update();
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

    private sealed class TransformStarRenderer : IScreenFilterStep
    {
        public EffectPriority Priority => EffectPriority.VeryLow;

        public bool Apply(in ScreenFilterRendererContext ctx)
        {
            return DrawTransformStars(ctx.ScreenTarget, ctx.ScreenTargetSwap);
        }
    }

    private static bool DrawTransformStars(RenderTarget2D screen, RenderTarget2D screenSwap)
    {
        if (TransformAnimation.ActiveParticleCount <= 0)
        {
            return false;
        }

        var sb = Main.spriteBatch;
        var device = Main.graphics.GraphicsDevice;

        const float start_range = 0.1f;

        var texture = Assets.Star.Asset.Value;

        var origin = texture.Size() * 0.5f;

        device.SetRenderTarget(screenSwap);
        device.Clear(Color.Transparent);

        sb.Begin();
        {
            sb.Draw(screen, Vector2.Zero, Color.White);
        }
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.Multiplicative, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        {
            foreach (var index in TransformAnimation)
            {
                var anim = TransformAnimation[index];

                var scale = Utils.Remap(anim.LifeTime, 0f, start_range, 0f, 1f) * Utils.Remap(anim.LifeTime, start_range, 1f, 1f, 0f);

                scale = 1f - MathF.Pow(1f - scale, 3f);

                DrawFlare(anim, new Color(0, 215, 215), scale, 0f);
                DrawFlare(anim, new Color(40, 215, 215), scale, 4f);
                DrawFlare(anim, new Color(110, 215, 215), scale, 16f);
            }
        }
        sb.End();

        return true;

        void DrawFlare(TransformStar anim, Color color, float baseScale, float decrement)
        {
            var position = anim.Position.Floor();

            var flareScale = baseScale * ((texture.Width - (decrement * 4f)) / texture.Width);
            sb.Draw(texture, position - Main.screenPosition, null, color, 0f, origin, flareScale, SpriteEffects.None, 0f);
        }
    }

    [GameInterfaceLayers.Before(GameInterfaceLayers.CURSOR, InterfaceScaleType.UI, Name = $"{nameof(RosemaryVanity)}: Siffrin UI Particles")]
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
