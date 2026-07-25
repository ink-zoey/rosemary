using Daybreak.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rosemary.Core;
using Terraria;

namespace Rosemary.Vanity.Content;

public static partial class SiffrinParticles
{
    public record struct BlackStar(Vector2 Position, byte Style, byte Frame, int FrameCounter) : IUpdatingParticle
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

    public static UpdatingParticleHandler<BlackStar> BlackStars { get; set; } = new(128);

    [ModSystemHooks.PostUpdateDusts]
    private static void UpdateParticles()
    {
        BlackStars.Update();
    }

    [ParticleLayers.UnderPlayers]
    private static void DrawParticlesUnderPlayers(SpriteBatch sb)
    {
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        {
            DrawStars();
        }
        sb.End();

        return;

        void DrawStars()
        {
            if (BlackStars.ActiveParticleCount <= 0)
            {
                return;
            }

            var texture = Assets.Vanity.Star.Asset.Value;

            var origin = new Vector2(9f);

            foreach (var index in BlackStars)
            {
                var star = BlackStars[index];

                var frame = texture.Frame(6, 4, star.Frame, star.Style);

                sb.Draw(texture, star.Position - Main.screenPosition, frame, Color.Black, 0f, origin, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}
