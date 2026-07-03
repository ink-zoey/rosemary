using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework;
using Rosemary.Core;

namespace Rosemary.Content.Elk;

public static class ElkParticles
{
    public record struct Spark(Vector2 Position, Vector2 Velocity, float Scale) : IUpdatingParticle
    {
        bool IUpdatingParticle.Update()
        {
            Position += Velocity;

            Scale -= 0.01f;

            return Scale > 0f;
        }
    }

    public static UpdatingParticleHandler<Spark> Sparks { get; set; } = new(512);

    [ModSystemHooks.PostUpdateDusts]
    private static void UpdateParticles()
    {
        Sparks.Update();
    }
}
