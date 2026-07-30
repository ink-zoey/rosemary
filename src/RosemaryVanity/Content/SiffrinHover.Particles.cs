using Daybreak.Hooks;
using Daybreak.Rendering;
using Daybreak.Rendering.Buffers;
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

    public record struct TransformStar(Vector2 Position, float LifeTime, int FrameCounter) : IUpdatingParticle
    {
        bool IUpdatingParticle.Update()
        {
            FrameCounter++;

            if (FrameCounter >= 3)
            {
                LifeTime += 0.1f;
                FrameCounter = 0;
            }

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

    private static Vector2 priorScreenPosition;

    private static bool DrawTransformStars(RenderTarget2D screen, RenderTarget2D screenSwap)
    {
        if (TransformAnimation.ActiveParticleCount <= 0)
        {
            priorScreenPosition = Main.screenPosition;

            return false;
        }

        var sb = Main.spriteBatch;
        var device = Main.graphics.GraphicsDevice;

        const float start_range = 0.2f;

        var texture = Assets.Star.Asset.Value;

        var origin = texture.Size() * 0.5f;

        using var lease = ScreenspaceTargetProvider.Shared.Create(device);

        device.SetRenderTarget(lease.Target);
        device.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        {
            foreach (var index in TransformAnimation)
            {
                var anim = TransformAnimation[index];

                var scale = Utils.Remap(anim.LifeTime, 0f, start_range, 0f, 1f) * Utils.Remap(anim.LifeTime, start_range, 1f, 1f, 0f);

                scale -= 0.15f;

                scale = MathF.Saturate(scale);

                scale = 1f - MathF.Pow(1f - scale, 3f);

                DrawFlare(anim, Color.White, scale, 0f);
            }
        }
        sb.End();

        device.SetRenderTarget(screenSwap);
        device.Clear(Color.Transparent);

        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        {
            sb.Draw(screen, Vector2.Zero, Color.White);
        }
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.Multiplicative, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);
        {
            foreach (var index in TransformAnimation)
            {
                var anim = TransformAnimation[index];

                var scale = Utils.Remap(anim.LifeTime, 0f, start_range, 0f, 1f) * Utils.Remap(anim.LifeTime, start_range, 1f, 1f, 0f);

                scale = 1f - MathF.Pow(1f - scale, 3f);

                DrawFlare(anim, new Color(0, 215, 215), scale, 0f);
                DrawFlare(anim, new Color(40, 215, 215), scale, 4f);
                DrawFlare(anim, new Color(80, 215, 215), scale, 16f);
            }
        }
        sb.End();
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);
        {
            var effect = Assets.Vanity.TransformStarOutline.CreateTransformStarOutlineShader();

            effect.Parameters.StepSize = 2f;

            effect.Apply();

            // Inaccuracy factor, looks worse without it as our eyes perceive the outline weirdly.
            var position = -(Main.screenPosition - priorScreenPosition) * 0.15f;

            sb.Draw(lease.Target, position, Color.Red);
        }
        sb.End();

        priorScreenPosition = Main.screenPosition;

        return true;

        void DrawFlare(TransformStar anim, Color color, float baseScale, float decrement)
        {
            var position = anim.Position.Floor();

            var flareScale = baseScale - ((decrement * 4f) / texture.Width);
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
