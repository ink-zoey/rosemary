using Daybreak.Hooks;
using GoldMeridian.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Common;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Shaders;
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
        ///     If <see langword="true"/> for a given item, upon interaction with shimmer the item will pause, then be violently ejected.<br/>
        ///     Made for use with <see cref="IViolentShimmerReactant"/> applied on the <see cref="ModItem"/>.
        /// </summary>
        public static bool[] ViolentShimmerReaction => violentShimmerReaction;
    }

    [OnLoad]
    private static void Load_ViolentShimmerReaction()
    {
        On_WorldItem.Shimmering += Shimmering_ViolentShimmerReaction;
        IL_WorldItem.MoveInWorld += MoveInWorld_ViolentShimmerReaction;
        IL_Main.DrawItem += DrawItem_ViolentShimmerReaction;
    }

    private static void DrawItem_ViolentShimmerReaction(ILContext il)
    {
        var c = new ILCursor(il);

        var itemIndex = -1;  // arg
        var colorIndex = -1; // loc

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdarg(out itemIndex),
            i => i.MatchLdloc(out _),
            i => i.MatchCallvirt<WorldItem>(nameof(WorldItem.GetAlpha)),
            i => i.MatchStloc(out colorIndex)
        );

        c.EmitLdarg(itemIndex);
        c.EmitLdloca(colorIndex);
        c.EmitDelegate(
            static (WorldItem item, ref Color color) =>
            {
                if (!ItemID.Sets.ViolentShimmerReaction[item.type]
                 || item.ShimmerData is null
                 || item.ShimmerData.WaveProgress < 0f
                 || !item.shimmerWet)
                {
                    return;
                }

                var interpolator = 1f - MathF.Pow(1f - item.ShimmerData.WaveProgress, 2f);

                color = Color.Lerp(color, Color.White, interpolator);
            }
        );
    }

    [GlobalItemHooks.PostDrawInWorld]
    private static void PostDrawInWorld_ViolentShimmerReaction(WorldItem item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
    {
        if (!ItemID.Sets.ViolentShimmerReaction[item.type]
         || item.ShimmerData is null
         || item.ShimmerData.WaveProgress < 0f
         || !item.shimmerWet)
        {
            return;
        }

        Main.instance.DrawItem_GetBasics(item.inner, whoAmI, out var texture, out var frame, out _);

        var origin = frame.Size() * 0.5f;

        var off = new Vector2((item.width * 0.5f) - origin.X, item.height - frame.Height);

        var position = (item.position + origin + off) - Main.screenPosition;

        var color = alphaColor;
        color.A = 0;

        var interpolator = 1f - MathF.Pow(1f - item.ShimmerData.WaveProgress, 3f);

        color *= interpolator;

        const float freq = 7f;
        const float amp = 0.5f;

        var time = Main.GlobalTimeWrappedHourly * freq;

        var wave = ((time % 1f) - 0.5f) * 2f;
        wave = (MathF.Abs(wave) - 0.5f) * 2f;

        scale *= 1f + (wave * amp * interpolator);

        spriteBatch.Draw(texture, position, frame, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }


    private const float increment_violent_shimmer_reaction = 0.006f;

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

                var modifier = new PunchCameraModifier(curPosition, new Vector2(1f, 0f), 4f, 7f, (int)(1f / increment_violent_shimmer_reaction) + 20, 1200f, $"{nameof(Rosemary)}: SHIMMER_VIOLENT_WARNING");
                Main.instance.CameraModifiers.Add(modifier);

                SoundEngine.PlaySound(
                    Assets.Elk.Shimmer.Burn.Asset with
                    {
                        PauseBehavior = PauseBehavior.PauseWithGame,
                        MaxInstances = 3,
                    },
                    curPosition,
                    _ => Main.tile[item.Bottom.ToTileCoordinates()].HasShimmer
                      || Main.tile[item.Top.ToTileCoordinates()].HasShimmer,
                    3100f
                );

                SoundEngine.PlaySound(
                    Assets.Elk.Shimmer.Scowl.Asset with
                    {
                        PauseBehavior = PauseBehavior.PauseWithGame,
                        MaxInstances = 3,
                    },
                    curPosition,
                    _ => Main.tile[item.Bottom.ToTileCoordinates()].HasShimmer
                      || Main.tile[item.Top.ToTileCoordinates()].HasShimmer,
                    3600f
                );
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
        for (var j = 0; j < 16; j++)
        {
            var position = self.Bottom.ToTileCoordinates();
            position.Y -= j + 1;

            if (Main.tile[position].HasShimmer)
            {
                continue;
            }

            position.Y += 1;

            var liquidLevel = (float)Main.tile[position].LiquidAmount / byte.MaxValue;
            liquidLevel = (1f - liquidLevel) * 16f;

            curPosition = position.ToWorldCoordinates(self.Bottom.X % 16f, liquidLevel);

            break;
        }

        // Find the bounds of the current pool
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

        self.ShimmerData.WaveProgress += increment_violent_shimmer_reaction;

        var progress = self.ShimmerData.WaveProgress;

        var subSurface = false;

        var above = self.Center.ToTileCoordinates();
        above.Y -= 1;
        if (Main.tile[above].HasShimmer)
        {
            var dist = curPosition == self.Bottom
              ? 1f
              : MathF.Saturate(MathF.Abs(self.Center.Y - curPosition.Y) / 80f);

            self.velocity.Y = -22f * dist;
            subSurface = true;
        }
        else
        {
            self.velocity *= 0.5f;
        }

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

        SoundEngine.PlaySound(
            Assets.Elk.Shimmer.Ejection.Asset with
            {
                PauseBehavior = PauseBehavior.PauseWithGame,
                MaxInstances = 3,
            },
            curPosition,
            attenuationDistance: 5500f
        );

        if (self.inner.ModItem is IViolentShimmerReactant reactant
         && reactant.Ejection(self, subSurface))
        {
            self.ClearOut();
        }

        return;

        void PassiveEffects()
        {
            // Acid bubbles
            ElkShimmerParticles.Bubbles +=
                new ElkShimmerParticles.ShimmerBubble(
                    Rand.Next(self.Hitbox),
                    GetShimmerSplashColor(),
                    Rand.Next(-1f, 1f),
                    Rand.Next((byte)1, (byte)4),
                    0
                );

            WaterShaderData.Instance.QueueRipple(Rand.Next(self.Hitbox), Rand.Next(0.15f, 0.85f), RippleShape.Square, MathF.PiOver4);

            // Surface droplets
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

            if (subSurface)
            {
                return;
            }

            // Dust moving inward
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

            // Ripples closing in
            var rippleOffset = new Vector2((1f - progress) * 700f, Rand.Next(-8f, 8f));
            var size = new Vector2(MathF.Max(50f * MathF.Pow(progress, 3), 6f));

            var strength = MathF.Max(MathF.Pow(progress, 2), 0.8f);

            WaterShaderData.Instance.QueueRipple(curPosition + rippleOffset, Rand.Next(0.75f, 1f) * strength, size, RippleShape.Square, MathF.PiOver4);
            WaterShaderData.Instance.QueueRipple(curPosition - rippleOffset, Rand.Next(0.75f, 1f) * strength, size, RippleShape.Square, MathF.PiOver4);

            // Inward spikes
            if (!(rippleOffset.X >= 85f))
            {
                return;
            }

            curPosition.X -= 16f;

            SpawnSpike(rippleOffset.X, Rand.Next(32f, 64f), Rand.Next(0.04f, 0.07f));
            SpawnSpike(-rippleOffset.X, Rand.Next(32f, 64f), Rand.Next(0.04f, 0.07f));
        }

        void EjectEffects()
        {
            ElkShimmerParticles.Rings += new ElkShimmerParticles.ExpandingRing(self.Center, 0.1f, 0.02f, 0f, 0.04f);

            // Camera shake with lingering effect
            var strong = new PunchCameraModifier(curPosition, new Vector2(0f, -1f), 35f, 8f, 45, 4500f, $"{nameof(Rosemary)}: SHIMMER_VIOLENT_WRONG");
            var lingering = new PunchCameraModifier(curPosition, new Vector2(0f, -1f), 2f, 9f, 200, 7000f, $"{nameof(Rosemary)}: SHIMMER_VIOLENT_AFTERSHOCK");
            Main.instance.CameraModifiers.Add(strong);
            Main.instance.CameraModifiers.Add(lingering);

            if (subSurface)
            {
                return;
            }

            // Giant splash with a center bias
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

            curPosition.X -= 16f;

            // Hand-picked ejection values
            SpawnSpike(-86f, 48f, 0.08f);
            SpawnSpike(-70f, 64f, 0.025f);
            SpawnSpike(-48f, 16f, 0.04f);
            SpawnSpike(-16f, 128f, 0.03f);
            SpawnSpike(0f, 200f, 0.055f);
            SpawnSpike(16f, 128f, 0.03f);
            SpawnSpike(48f, 16f, 0.04f);
            SpawnSpike(70f, 64f, 0.025f);
            SpawnSpike(86f, 48f, 0.08f);
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

    [GlobalNPCHooks.PreAI]
    private static bool PreAI_ViolentShimmerReaction_Faelings(NPC npc)
    {
        if (npc.aiStyle != NPCAIStyleID.Firefly
         || npc.type != NPCID.Shimmerfly)
        {
            return true;
        }

        var count = 0;
        var zero = Vector2.Zero;
        foreach (var item in Main.ActiveItems)
        {
            var diff = npc.Center - item.Center;

            if (item.ShimmerData is null || item.ShimmerData.WaveProgress <= 0f || diff.Length() >= 900f)
            {
                continue;
            }

            count++;
            zero += diff.Normalized;
        }

        if (zero == Vector2.Zero)
        {
            return true;
        }

        zero /= count;
        zero *= 2f;

        npc.velocity += zero;

        if (npc.velocity.Length() > 7f)
        {
            npc.velocity.Magnitude = 7f;
        }

        npc.localAI[0] = 10f;
        npc.netUpdate = true;

        return true;
    }
}
