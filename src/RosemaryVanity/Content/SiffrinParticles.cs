using Daybreak.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rosemary.Core;
using System;
using Terraria;
using Terraria.ModLoader;

namespace Rosemary.Vanity.Content;

[Autoload(Side = ModSide.Client)]
public static class SiffrinParticles
{
    public record struct NegativeSmoke(Vector2 Position, Vector2 Velocity, float Scale, Color Color) : IUpdatingParticle
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

    public static UpdatingParticleHandler<NegativeSmoke> Sparks { get; set; } = new(512);

    [ModSystemHooks.PostUpdateDusts]
    private static void UpdateParticles()
    {
        Sparks.Update();
    }

    [ParticleLayers.OverPlayers]
    private static void DrawParticlesOverPlayers(SpriteBatch sb)
    {
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        {
            DrawSparks();
        }
        sb.End();

        return;

        void DrawSparks()
        {

        }
    }
}
