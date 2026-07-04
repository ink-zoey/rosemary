using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Core;
using System;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Renderers;

namespace Rosemary.Content.Elk;

public static class ElkParticles
{
    public record struct Spark(Vector2 Position, Vector2 Velocity, float Scale, Color Color, byte Style) : IUpdatingParticle
    {
        bool IUpdatingParticle.Update()
        {
            Position += Velocity;

            var newVelocity = Velocity;

            newVelocity.Y += (1f - (Scale * 0.15f)) * 0.04f;

            Velocity = newVelocity;

            Scale -= 0.085f;

            return Scale > 0f;
        }
    }

    public static UpdatingParticleHandler<Spark> Sparks { get; set; } = new(512);

    [OnLoad]
    private static void Load()
    {
        IL_Main.DoDraw += DoDraw_DrawParticles;
    }

    private static void DoDraw_DrawParticles(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.ParticleSystem_World_OverPlayers)),
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
            i => i.MatchCallvirt<ParticleRenderer>(nameof(ParticleRenderer.Draw))
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
            i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End))
        );

        c.EmitLdsfld(
            typeof(Main).GetField(
                nameof(Main.spriteBatch),
                BindingFlags.Static | BindingFlags.Public
            )!
        );
        c.EmitDelegate(DrawForegroundParticles);
    }

    [ModSystemHooks.PostUpdateDusts]
    private static void UpdateParticles()
    {
        Sparks.Update();
    }

    private static void DrawForegroundParticles(SpriteBatch sb)
    {
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        {
            DrawSparks();
        }
        sb.End();

        return;

        void DrawSparks()
        {
            if (Sparks.ActiveParticleCount <= 0)
            {
                return;
            }

            var texture = Assets.Elk.Particles.Spark.Asset.Value;

            var origin = new Vector2(8f);

            var starFrame = new Rectangle(48, 0, 39, 39);

            var starOrigin = starFrame.Size() * 0.5f;

            foreach (var index in Sparks)
            {
                var spark = Sparks[index];

                var position = spark.Position - Main.screenPosition;

                var frame = new Rectangle(16 * spark.Style, 0, 16, 40);

                var color = spark.Color * spark.Scale;

                var rotation = spark.Velocity.ToRotation() + MathHelper.PiOver2;

                var scale = 1f - MathF.Pow(1f - MathF.Min(spark.Scale, 1f), 1.5f);

                var size = new Vector2(0.24f * scale, 0.7f * spark.Scale);

                sb.Draw(texture, position, frame, color, rotation, origin, size, SpriteEffects.None, 0f);

                var starScale = MathF.Max(spark.Scale - 0.6f, 0f) * 1.4f * (spark.Style * 0.33f);

                starScale = MathF.Min(starScale, 0.9f);

                sb.Draw(texture, position, starFrame, spark.Color, 0f, starOrigin, starScale, SpriteEffects.None, 0f);
            }
        }
    }
}
