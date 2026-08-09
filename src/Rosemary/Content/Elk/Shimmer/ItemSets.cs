using Daybreak.Hooks;
using Daybreak.MonoMod;
using GoldMeridian.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Common;
using Rosemary.Core;
using System;
using System.Collections.Generic;
using Daybreak.Rendering;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Liquid;
using Terraria.GameContent.Shaders;
using Terraria.Graphics;
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

    private record struct ShimmerSpike(Point Position, int XOffset, float Height, float LifeTime, float LifeTimeIncrement) : IUpdatingParticle
    {
        public bool Update()
        {
            LifeTime += LifeTimeIncrement;

            return LifeTime <= 1f;
        }
    }

    private static UpdatingParticleHandler<ShimmerSpike> spikes = new(128);

    private record struct ShimmerSear(Vector2 Position, float Rotation, float LifeTime, float LifeTimeIncrement) : IUpdatingParticle
    {
        public bool Update()
        {
            LifeTime += LifeTimeIncrement;

            if (Main.GameUpdateCount % 8 != 0)
            {
                return LifeTime <= 1f;
            }

            var velocity = Rotation.ToRotationVector2() * 6f;

            sparks += new ShimmerSearSpark(Position, velocity, Rotation, LifeTime * 0.4f, 0.04f);
            sparks += new ShimmerSearSpark(Position, -velocity, Rotation, LifeTime * 0.4f, 0.04f);

            return LifeTime <= 1f;
        }
    }

    private record struct ShimmerSearSpark(Vector2 Position, Vector2 Velocity, float Rotation, float LifeTime, float LifeTimeIncrement) : IUpdatingParticle
    {
        public bool Update()
        {
            Velocity *= 0.91f;

            Position += Velocity;

            LifeTime += LifeTimeIncrement;

            return LifeTime <= 1f;
        }
    }

    private static UpdatingParticleHandler<ShimmerSear> sears = new(32);

    private static UpdatingParticleHandler<ShimmerSearSpark> sparks = new(128);

    [ModSystemHooks.ClearWorld]
    private static void ClearParticles_ViolentShimmerReaction()
    {
        spikes.Clear();
        sears.Clear();
        sparks.Clear();
    }

    [ModSystemHooks.PostUpdateDusts]
    private static void UpdateParticles_ViolentShimmerReaction()
    {
        spikes.Update();
        sears.Update();
        sparks.Update();
    }

    [OnLoad]
    private static void Load_ViolentShimmerReaction()
    {
        On_WorldItem.Shimmering += Shimmering_ViolentShimmerReaction;
        IL_WorldItem.MoveInWorld += MoveInWorld_ViolentShimmerReaction;
        IL_LiquidRenderer.DrawShimmer += DrawShimmer_ViolentShimmerReaction;
        On_Main.DrawInfernoRings += DrawInfernoRings_DrawFlareParticles;
    }

    private static void DrawInfernoRings_DrawFlareParticles(On_Main.orig_DrawInfernoRings orig, Main self)
    {
        orig(self);

        var sb = Main.spriteBatch;

        using var _ = sb.Scope();

        var texture = TextureAssets.Extra[ExtrasID.NinetyEight].Value;
        var origin = texture.Size() * 0.5f;

        var size = new Vector2(0.4f, 2.6f);

        var color = new Color(179, 133, 255, 40);

        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        {
            foreach (var index in sears)
            {
                var sear = sears[index];

                var position = sear.Position - Main.screenPosition;

                var scale = size;

                scale *=
                    (1f - MathF.Pow(sear.LifeTime, 5f))
                  * (1 - MathF.Pow(1f - sear.LifeTime, 15f));

                sb.Draw(texture, position, null, color, sear.Rotation + MathF.PiOver2, origin, scale, SpriteEffects.None, 0f);

                scale.Y *= 0.7f;
                sb.Draw(texture, position, null, color, sear.Rotation + MathF.PiOver2, origin, scale, SpriteEffects.None, 0f);
            }

            size.Y = 0.6f;

            foreach (var index in sparks)
            {
                var spark = sparks[index];

                var position = spark.Position - Main.screenPosition;

                var scale = size;

                scale *= (1f - MathF.Pow(spark.LifeTime, 2f));

                sb.Draw(texture, position, null, color, spark.Rotation, origin, scale, SpriteEffects.None, 0f);
                sb.Draw(texture, position, null, color, spark.Rotation + MathF.PiOver2, origin, scale * 0.3f, SpriteEffects.None, 0f);

                scale.Y *= 0.7f;
                sb.Draw(texture, position, null, color, spark.Rotation, origin, scale, SpriteEffects.None, 0f);
                sb.Draw(texture, position, null, color, spark.Rotation + MathF.PiOver2, origin, scale * 0.3f, SpriteEffects.None, 0f);
            }
        }
        sb.End();
    }

    private record struct ShimmerSpikeDrawItem(int Index, Vector2 Position, float Opacity);

    private static unsafe void DrawShimmer_ViolentShimmerReaction(ILContext il)
    {
        var c = new ILCursor(il);

        var spikesByPositionReference = c.AddVariable<Dictionary<Point, int>>();
        var spikesDrawCacheReference = c.AddVariable<List<ShimmerSpikeDrawItem>>();
        var useInnerFrameReference = c.AddVariable<bool>();

        var liquidRendererIndex = -1;  // arg
        var sourceRectangleIndex = -1; // loc

        var liquidCacheCurrentIndex = -1; // loc

        var xIndex = -1; // loc
        var yIndex = -1; // loc

        var isBackgroundDrawIndex = -1; // arg

        c.EmitStaticDelegateUnsafe(
            static () =>
            {
                var dict = new Dictionary<Point, int>();

                foreach (var index in spikes)
                {
                    dict[spikes[index].Position] = index;
                }

                return dict;
            }
        );
        c.EmitStloc(spikesByPositionReference);

        c.EmitStaticDelegateUnsafe(
            static () => new List<ShimmerSpikeDrawItem>()
        );
        c.EmitStloc(spikesDrawCacheReference);

        c.GotoNext(i => i.MatchLdarg(out liquidRendererIndex));

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdloca(out sourceRectangleIndex),
            i => i.MatchLdcI4(1280),
            i => i.MatchStfld<Rectangle>(nameof(Rectangle.Y))
        );

        var c2 = c.Clone();
        {
            c2.GotoNext(i => i.MatchCall<Lighting>(nameof(Lighting.GetCornerColors)));

            c2.GotoPrev(
                i => i.MatchLdloc(out xIndex),
                i => i.MatchLdloc(out yIndex)
            );

            c2.GotoPrev(
                i => i.MatchLdloc(out liquidCacheCurrentIndex),
                i => i.MatchLdfld<LiquidRenderer.LiquidDrawCache>(nameof(LiquidRenderer.LiquidDrawCache.Opacity)),
                i => i.MatchLdarg(out isBackgroundDrawIndex)
            );
        }

        c.EmitLdloc(liquidCacheCurrentIndex);
        c.EmitLdarg(liquidRendererIndex);
        c.EmitLdloc(xIndex);
        c.EmitLdloc(yIndex);
        c.EmitLdloca(sourceRectangleIndex);
        c.EmitLdarg(isBackgroundDrawIndex);
        c.EmitLdloc(spikesByPositionReference);
        c.EmitLdloc(spikesDrawCacheReference);
        c.EmitDelegate(
            static (
                LiquidRenderer.LiquidDrawCache* liquidCache,
                LiquidRenderer renderer,
                int i,
                int j,
                ref Rectangle source,
                bool isBackgroundDraw,
                Dictionary<Point, int> spikesByPosition,
                List<ShimmerSpikeDrawItem> spikeCache
            ) =>
            {
                var tilePosition = new Point(i, j);

                if (!spikesByPosition.TryGetValue(tilePosition, out var index))
                {
                    return false;
                }

                // Have the liquid use a non-surface frame
                source.Y = 60 + renderer._animationFrame * 80;

                var opacity = liquidCache->Opacity * (isBackgroundDraw ? 1f : 0.75f);

                var position =
                    tilePosition.ToWorldCoordinates(Vector2.Zero)
                  + liquidCache->LiquidOffset
                  - (isBackgroundDraw ? Main.backWaterTarget.Position : Main.waterTarget.Position);

                position.Y += 2f;

                spikeCache.Add(new ShimmerSpikeDrawItem(index, position, opacity));

                return true;
            }
        );
        c.EmitStloc(useInnerFrameReference);

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdloc(liquidCacheCurrentIndex),
            i => i.MatchLdfld<LiquidRenderer.LiquidDrawCache>(nameof(LiquidRenderer.LiquidDrawCache.SourceRectangle)),
            i => i.MatchStloc(sourceRectangleIndex)
        );

        c.EmitLdloc(useInnerFrameReference);
        c.EmitLdloca(sourceRectangleIndex);
        c.EmitDelegate(
            static (
                bool useInnerFrame,
                ref Rectangle source
            ) =>
            {
                if (!useInnerFrame)
                {
                    return;
                }

                source.Y = 60;
            }
        );

        c.EmitLdcI4(0);
        c.EmitStloc(useInnerFrameReference);

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdsfld<Main>(nameof(Main.tileBatch)),
            i => i.MatchCallvirt<TileBatch>(nameof(TileBatch.End))
        );

        c.MoveAfterLabels();

        c.EmitLdloc(spikesDrawCacheReference);
        c.EmitDelegate(
            static (
                List<ShimmerSpikeDrawItem> spikeCache
            ) =>
            {
                var tb = Main.tileBatch;

                var texture = Assets.Elk.Particles.ShimmerSpike.Asset.Value;

                var backingFrame = texture.Frame(2, 1, 0, 0);
                var frontFrame = texture.Frame(2, 1, 1, 0);

                backingFrame.Height = 64;
                frontFrame.Height = 64;

                var overlayFrame = new Rectangle(16, 64, 16, 10);

                foreach (var (index, position, opacity) in spikeCache)
                {
                    var spike = spikes[index];

                    var height = spike.Height;

                    height *= MathF.Sin(spike.LifeTime * MathF.PI);

                    var dest = new Rectangle((int)position.X + spike.XOffset, (int)((position.Y - height)), 16, (int)height);

                    var colors = GetShimmerColors(spike, height, position, opacity, false);

                    tb.Draw(texture, dest, backingFrame, colors);

                    colors = GetShimmerColors(spike, height, position, opacity, true);

                    tb.Draw(texture, dest, frontFrame, colors);

                    var tilePosition = position.ToTileCoordinates();
                    LiquidRenderer.SetShimmerVertexColors_Sparkle(ref colors, opacity, tilePosition.X, tilePosition.Y, true);

                    var overlayDest = new Rectangle((int)position.X, (int)position.Y - 1, 16, 16 - ((int)position.Y % 16));
                    tb.Draw(texture, overlayDest, overlayFrame, colors);
                }

                return;

                static VertexColors GetShimmerColors(ShimmerSpike spike, float height, Vector2 position, float opacity, bool useSparkleColor)
                {
                    var tilePosition = position.ToTileCoordinates();

                    var positions = new Point[]
                    {
                        new(tilePosition.X, (int)((position.Y - height) / 16f)),
                        new(tilePosition.X + 1, (int)((position.Y - height) / 16f)),
                        new(tilePosition.X, tilePosition.Y + 1),
                        new(tilePosition.X + 1, tilePosition.Y + 1),
                    };

                    var colors = new VertexColors(Color.White);

                    if (useSparkleColor)
                    {
                        colors.TopLeftColor = LiquidRenderer.GetShimmerGlitterColor(true, positions[0].X, positions[0].Y);
                        colors.TopRightColor = LiquidRenderer.GetShimmerGlitterColor(true, positions[1].X, positions[1].Y);
                        colors.BottomLeftColor = LiquidRenderer.GetShimmerGlitterColor(true, positions[2].X, positions[2].Y);
                        colors.BottomRightColor = LiquidRenderer.GetShimmerGlitterColor(true, positions[3].X, positions[3].Y);
                    }
                    else
                    {
                        colors.TopLeftColor = new Color(colors.TopLeftColor.ToVector4() * LiquidRenderer.GetShimmerBaseColor(positions[0].X, positions[0].Y));
                        colors.TopRightColor = new Color(colors.TopRightColor.ToVector4() * LiquidRenderer.GetShimmerBaseColor(positions[1].X, positions[1].Y));
                        colors.BottomLeftColor = new Color(colors.BottomLeftColor.ToVector4() * LiquidRenderer.GetShimmerBaseColor(positions[2].X, positions[2].Y));
                        colors.BottomRightColor = new Color(colors.BottomRightColor.ToVector4() * LiquidRenderer.GetShimmerBaseColor(positions[3].X, positions[3].Y));
                    }

                    colors *= opacity;

                    return colors;
                }
            }
        );
    }

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

                sears += new ShimmerSear(curPosition, Rand.Next(-0.12f, 0.12f), 0f, 0.035f);
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

        var startingPosition = self.Bottom;
        startingPosition.X -= 8f;
        for (var j = 0; j < 8; j++)
        {
            var position = self.Bottom.ToTileCoordinates();
            position.Y -= j + 1;

            if (Main.tile[position].HasShimmer)
            {
                continue;
            }

            startingPosition.Y -= j * 16f;
            break;
        }

        self.velocity = Vector2.Zero;

        self.ShimmerData.WaveProgress += 0.035f;

        var progress = self.ShimmerData.WaveProgress;

        var rippleOffset = new Vector2((1f - progress) * 500f, Rand.Next(-8f, 8f));
        WaterShaderData.Instance.QueueRipple(startingPosition + rippleOffset, Rand.Next(0.75f, 1f) * (1f - MathF.Pow(1f - progress, 2)), RippleShape.Square, MathF.PiOver4);
        WaterShaderData.Instance.QueueRipple(startingPosition - rippleOffset, Rand.Next(0.75f, 1f) * (1f - MathF.Pow(1f - progress, 2)), RippleShape.Square, MathF.PiOver4);

        if (progress < 1f)
        {
            return;
        }

        var velocity = -self.velocity;
        velocity.Y = -9f;

        self.velocity = velocity;

        self.ShimmerData.WaveProgress = -1f;

        SpawnSpike(-86f, 48f, 0.08f);
        SpawnSpike(-70f, 32f, 0.025f);
        SpawnSpike(-48f, 16f, 0.04f);
        SpawnSpike(-16f, 32f, 0.03f);
        SpawnSpike(0f, 64f, 0.055f);
        SpawnSpike(16f, 32f, 0.03f);
        SpawnSpike(48f, 16f, 0.04f);
        SpawnSpike(70f, 32f, 0.025f);
        SpawnSpike(86f, 48f, 0.08f);

        return;

        void SpawnSpike(float offset, float height, float speed)
        {
            var position = startingPosition;

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

            spikes += new ShimmerSpike(tilePosition, innerOffset, height, 0f, speed);
        }
    }
}
