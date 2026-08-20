using GoldMeridian.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rosemary.Common;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content.Elk;

public abstract class ShimmerReactionGore : ModGore, ICustomDrawGore
{
    [ExtensionDataFor<Gore>("ShimmerData")]
    internal sealed class ShimmerReactionData
    {
        public required bool Shimmering { get; set; }

        public required bool SpawnedSubSurface { get; set; }
    }

    public override void SetStaticDefaults()
    {
        ChildSafety.SafeGore[Type] = true;
    }

    public override void OnSpawn(Gore gore, IEntitySource source)
    {
        gore.ShimmerData = new ShimmerReactionData
        {
            Shimmering = false,
            SpawnedSubSurface = false,
        };
    }

    public override bool Update(Gore gore)
    {
        if (gore.ShimmerData is null)
        {
            return true;
        }

        var subSurface = gore.ShimmerData.SpawnedSubSurface;

        var solid = Collision.SolidCollision(gore.position, (int)gore.Width, (int)gore.Height);

        var wasShimmering = gore.ShimmerData.Shimmering;
        var shimmering = Collision.WetCollision(gore.position, (int)gore.Width, (int)gore.Height) && Collision.shimmer;

        var ratio = (gore.alpha / (float)byte.MaxValue);

        if (solid)
        {
            gore.alpha += 3;
        }
        else if (Rand.NextBoolean(12))
        {
            var dust = Dust.NewDustPerfect(
                gore.Center,
                DustID.ShimmerSplash,
                new Vector2(Rand.Next(-1f, 1f), Rand.Next(-4f, -2f)),
                0,
                GetShimmerSplashColor(),
                1.2f * (1f - MathF.Pow(ratio, 3f))
            );

            dust.noGravity = true;
        }

        if (gore.velocity.Y < 0f
         && !subSurface)
        {
            return true;
        }

        if (!wasShimmering
         && shimmering
         && !subSurface)
        {
            gore.ShimmerData.Shimmering = true;
            Shimmer();
        }

        if (subSurface)
        {
            gore.ShimmerData.Shimmering = shimmering;
        }

        if (gore.ShimmerData.Shimmering)
        {
            Shimmering();
        }

        return true;

        void Shimmer()
        {
            // Splash particles
            for (var i = 0; i < 10; i++)
            {
                var index = Dust.NewDust(
                    new Vector2(gore.position.X - 6f, gore.position.Y + (gore.Height * 0.5f) - 8f),
                    (int)gore.Width + 12,
                    24,
                    DustID.ShimmerSplash,
                    newColor: GetShimmerSplashColor(),
                    Scale: 0.8f
                );

                var dust = Main.dust[index];

                dust.velocity.Y -= 4f;
                dust.velocity.X *= 2.5f;
                dust.noGravity = true;
            }

            // TODO: SFX

            // Snap the position to the surface of the shimmer
            var curPosition = gore.Bottom;
            for (var j = 0; j < 8; j++)
            {
                var position = gore.Bottom.ToTileCoordinates();
                position.Y -= j + 1;

                if (Main.tile[position].HasShimmer)
                {
                    continue;
                }

                position.Y += 1;

                var liquidLevel = (float)Main.tile[position].LiquidAmount / byte.MaxValue;
                liquidLevel = (1f - liquidLevel) * 16f;

                curPosition = position.ToWorldCoordinates(gore.Bottom.X % 16f, liquidLevel);

                break;
            }

            gore.Center = curPosition;
        }

        void Shimmering()
        {
            gore.scale *= subSurface ? 0.99f : 0.98f;

            gore.velocity *= subSurface ? 0.93f : 0.2f;

            if (subSurface)
            {
                var topShimmer = Collision.WetCollision(gore.position, (int)gore.Width, 1) && Collision.shimmer;

                gore.velocity.Y -= topShimmer ? 0.38f : 0.2f;
            }
            else
            {
                gore.velocity.Y -= 0.16f;
            }

            gore.alpha += subSurface ? 2 : 3;

            var frame = (byte)(4 * ratio);

            if (frame < 1)
            {
                frame = 1;
            }

            WaterShaderData.Instance.QueueRipple(Rand.Next(gore.Hitbox), Rand.Next(0.15f, 0.85f) * (1f - ratio), RippleShape.Square, MathF.PiOver4);

            if (Rand.NextBoolean())
            {
                var rect = gore.Hitbox;

                rect.Inflate(3, 3);

                ElkShimmerParticles.Bubbles +=
                    new ElkShimmerParticles.ShimmerBubble(
                        Rand.Next(rect),
                        GetShimmerSplashColor(),
                        Rand.Next(-1f, 1f),
                        frame,
                        0
                    );
            }
        }

        static Color GetShimmerSplashColor()
        {
            return Rand.Next(6) switch
            {
                0 => new Color(255, 255, 210),
                1 => new Color(190, 245, 255),
                2 => new Color(255, 150, 255),
                _ => new Color(190, 175, 255),
            };
        }
    }

    public override Color? GetAlpha(Gore gore, Color lightColor) => Color.White * ((byte.MaxValue - gore.alpha) / (float)byte.MaxValue);

    void ICustomDrawGore.PostDraw(Gore gore, SpriteBatch sb, DrawData drawData)
    {
        drawData.color.A = 0;

        drawData.Draw(sb);
    }
}
