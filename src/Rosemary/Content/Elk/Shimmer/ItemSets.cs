using Daybreak.Hooks;
using Daybreak.MonoMod;
using Daybreak.Rendering;
using GoldMeridian.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Common;
using Rosemary.Core;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Liquid;
using Terraria.GameContent.Shaders;
using Terraria.Graphics;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content.Elk;

public static class ElkShimmerItemSets
{
    [ExtensionDataFor<WorldItem>("ShimmerData")]
    internal sealed class ShimmerReactionData
    {
        public required float WaveProgress { get; set; }
    }

    private static bool[] violentShimmerReaction = [];

    private static Mod Mod => ModContent.GetInstance<ModImpl>();

    [ModSystemHooks.ResizeArrays]
    private static void ResizeArrays()
    {
        violentShimmerReaction = CreateSet(nameof(violentShimmerReaction), false);

        return;

        static T[] CreateSet<T>(string name, T defaultState)
        {
            return ItemID.Sets.Factory.CreateNamedSet(Mod, name)
                         .RegisterCustomSet(defaultState);
        }
    }

    extension(ItemID.Sets)
    {
        /// <summary>
        ///     TODO
        /// </summary>
        public static bool[] ViolentShimmerReaction => violentShimmerReaction;
    }

    [OnLoad]
    private static void Load_ViolentShimmerReaction()
    {
        On_WorldItem.Shimmering += Shimmering_ViolentShimmerReaction;
        IL_WorldItem.MoveInWorld += MoveInWorld_ViolentShimmerReaction;
    }

    private const float increment_violent_shimmer_reaction = 0.03f;

    private static void MoveInWorld_ViolentShimmerReaction(ILContext il)
    {
        var c = new ILCursor(il);

        var itemIndex = -1; // arg

        c.GotoNext(
            MoveType.After,
            i => i.MatchCall(typeof(SoundEngine), nameof(SoundEngine.PlaySound)),
            i => i.MatchPop()
        );

        c.FindPrev(
            out _,
            i => i.MatchLdarg(out itemIndex),
            i => i.MatchLdflda<Entity>(nameof(Entity.position))
        );

        c.EmitLdarg(itemIndex);
        c.EmitDelegate(
            static (WorldItem item) =>
            {
                if (!ItemID.Sets.ViolentShimmerReaction[item.type])
                {
                    return;
                }

                item.ShimmerData ??= new ShimmerReactionData
                {
                    WaveProgress = 0f,
                };

                item.ShimmerData.WaveProgress = 0f;

                var curPosition = item.Bottom;
                for (var j = 0; j < 8; j++)
                {
                    var position = curPosition.ToTileCoordinates();
                    position.Y -= j + 1;

                    if (Main.tile[position].HasShimmer)
                    {
                        continue;
                    }

                    position.Y += 1;

                    var liquidLevel = (float)Main.tile[position].LiquidAmount / byte.MaxValue;
                    liquidLevel = (1f - liquidLevel) * 16f;

                    curPosition = position.ToWorldCoordinates(item.Bottom.X % 16f, liquidLevel);

                    break;
                }

                ElkShimmerParticles.Sears += new ElkShimmerParticles.ShimmerSear(curPosition, Rand.Next(-0.12f, 0.12f), 0f, increment_violent_shimmer_reaction);

                var modifier = new PunchCameraModifier(curPosition, new Vector2(1f, 0f), 4f, 7f, (int)(1f / increment_violent_shimmer_reaction) + 20, 900f, $"{nameof(Rosemary)}: SHIMMER_VIOLENT_WARNING");
                Main.instance.CameraModifiers.Add(modifier);
            }
        );
    }

    private static void Shimmering_ViolentShimmerReaction(On_WorldItem.orig_Shimmering orig, WorldItem self)
    {
        if (!ItemID.Sets.ViolentShimmerReaction[self.type])
        {
            orig(self);

            return;
        }

        self.shimmerTime = 0;
        self.shimmered = false;

        self.ShimmerData ??= new ShimmerReactionData
        {
            WaveProgress = 0f,
        };

        self.noGrabDelay = 90;

        if (self.ShimmerData.WaveProgress < 0f)
        {
            return;
        }

        var curPosition = self.Bottom;
        for (var j = 0; j < 8; j++)
        {
            var position = self.Bottom.ToTileCoordinates();
            position.Y -= j + 1;

            if (Main.tile[position].HasShimmer)
            {
                continue;
            }

            curPosition.Y -= j * 16f;
            break;
        }

        var minRange = -16f;
        var maxRange = 16f;
        for (var i = 0; i < 32; ++i)
        {
            var leftPosition = curPosition.ToTileCoordinates();
            var rightPosition = leftPosition;
            leftPosition.X -= i;
            rightPosition.X += i;

            var leftShimmer = Main.tile[leftPosition].HasShimmer;
            var rightShimmer = Main.tile[rightPosition].HasShimmer;

            if (!leftShimmer && !rightShimmer)
            {
                break;
            }

            if (leftShimmer)
            {
                minRange -= 16f;
            }
            if (rightShimmer)
            {
                maxRange += 16f;
            }
        }

        self.velocity = Vector2.Zero;

        self.ShimmerData.WaveProgress += increment_violent_shimmer_reaction;

        var progress = self.ShimmerData.WaveProgress;

        PassiveEffects();

        if (progress < 1f)
        {
            return;
        }

        var velocity = -self.velocity;
        velocity.Y = -20f;

        self.velocity = velocity;

        self.ShimmerData.WaveProgress = -1f;

        EjectEffects();

        return;

        void PassiveEffects()
        {
            var rippleOffset = new Vector2((1f - progress) * 500f, Rand.Next(-8f, 8f));
            var size = new Vector2(MathF.Max(50f * MathF.Pow(progress, 3), 6f));

            var strength = MathF.Max(MathF.Pow(progress, 2), 0.8f);

            WaterShaderData.Instance.QueueRipple(curPosition + rippleOffset, Rand.Next(0.75f, 1f) * strength, size, RippleShape.Square, MathF.PiOver4);
            WaterShaderData.Instance.QueueRipple(curPosition - rippleOffset, Rand.Next(0.75f, 1f) * strength, size, RippleShape.Square, MathF.PiOver4);


            if (rippleOffset.X >= 85f)
            {
                SpawnSpike(rippleOffset.X, Rand.Next(32f, 64f), Rand.Next(0.04f, 0.07f));
                SpawnSpike(-rippleOffset.X, Rand.Next(32f, 64f), Rand.Next(0.04f, 0.07f));
            }

            // Bubbles
            var dustOffset = new Vector2(Rand.Next(minRange, maxRange), 0f);

            var dust = Dust.NewDustPerfect(
                curPosition + dustOffset,
                DustID.ShimmerSplash,
                new Vector2(Rand.Next(-1f, 1f), Rand.Next(-13f, -5f)),
                0,
                GetShimmerSplashColor(),
                1.2f
            );

            dust.noGravity = true;

            dustOffset = Rand.NextUnitVector(Rand.Next(400f));
            dustOffset.Y = -MathF.Abs(dustOffset.Y);

            dust = Dust.NewDustPerfect(
                curPosition + dustOffset,
                DustID.ShimmerSplash,
                dustOffset.Normalized * -8f,
                0,
                GetShimmerSplashColor(),
                1.2f
            );

            dust.noGravity = true;
        }

        void EjectEffects()
        {
            for (var i = 0; i < 70; i++)
            {
                var dist = Rand.Next(-1f, 1f);

                var dustOffset = new Vector2((MathF.Pow(1f - MathF.Abs(dist), 3f)) * MathF.Sign(dist) * 120f, 0f);

                var dust = Dust.NewDustPerfect(
                    curPosition + dustOffset,
                    DustID.ShimmerSplash,
                    new Vector2(Rand.Next(-1f, 1f), Rand.Next(-50f * MathF.Abs(dist), -2f)),
                    0,
                    GetShimmerSplashColor(),
                    1.1f
                );

                dust.noGravity = true;
            }

            curPosition.X -= 8f;

            SpawnSpike(-86f, 48f, 0.08f);
            SpawnSpike(-70f, 64f, 0.025f);
            SpawnSpike(-48f, 16f, 0.04f);
            SpawnSpike(-16f, 128f, 0.03f);
            SpawnSpike(0f, 200f, 0.055f);
            SpawnSpike(16f, 128f, 0.03f);
            SpawnSpike(48f, 16f, 0.04f);
            SpawnSpike(70f, 64f, 0.025f);
            SpawnSpike(86f, 48f, 0.08f);

            ElkShimmerParticles.Rings += new ElkShimmerParticles.ExpandingRing(curPosition, 0.1f, 0.02f, 0f, 0.04f);

            var strong = new PunchCameraModifier(curPosition, new Vector2(0f, -1f), 35f, 8f, 45, 2500f, $"{nameof(Rosemary)}: SHIMMER_VIOLENT_WRONG");
            var lingering = new PunchCameraModifier(curPosition, new Vector2(0f, -1f), 2f, 9f, 200, 3900f, $"{nameof(Rosemary)}: SHIMMER_VIOLENT_AFTERSHOCK");
            Main.instance.CameraModifiers.Add(strong);
            Main.instance.CameraModifiers.Add(lingering);
        }

        void SpawnSpike(float offset, float height, float speed)
        {
            var position = curPosition;

            position.X += offset;

            var tilePosition = position.ToTileCoordinates();

            if (!Main.tile[tilePosition].HasShimmer)
            {
                return;
            }

            var innerOffset = (int)(position.X % 16);

            if (innerOffset > 8)
            {
                tilePosition.X += 1;
                innerOffset -= 16;
            }

            var style = Rand.NextBoolean(7, 3) ? (byte)0 : Rand.Next((byte)3);

            ElkShimmerParticles.Spikes += new ElkShimmerParticles.ShimmerSpike(tilePosition, innerOffset, height, style, 0f, speed);
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
}
