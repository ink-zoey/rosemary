using Daybreak.Hooks;
using Daybreak.MonoMod;
using GoldMeridian.CodeAnalysis;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Rosemary.Common;
using Rosemary.Core;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
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

    [ModSystemHooks.ClearWorld]
    private static void ClearParticles_ViolentShimmerReaction()
    {
        spikes.Clear();
    }

    [ModSystemHooks.PostUpdateDusts]
    private static void UpdateParticles_ViolentShimmerReaction()
    {
        spikes.Update();
    }

    [OnLoad]
    private static void Load_ViolentShimmerReaction()
    {
        On_WorldItem.Shimmering += Shimmering_ViolentShimmerReaction;
        IL_WorldItem.MoveInWorld += MoveInWorld_ViolentShimmerReaction;
        IL_LiquidRenderer.DrawShimmer += DrawShimmer_ViolentShimmerReaction;
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

                // It can be safely assumed that the opacity at the surface is 1
                var opacity = liquidCache->Opacity * (isBackgroundDraw ? 1f : 0.75f);

                // drawOffset can be disregarded as it is a retro lighting relic
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

        self.ShimmerData.WaveProgress += 0.04f;

        var progress = self.ShimmerData.WaveProgress;

        var rippleOffset = new Vector2((1f - progress) * 300f, 0f);
        WaterShaderData.Instance.QueueRipple(startingPosition + rippleOffset, 0.8f, RippleShape.Square, MathF.PiOver4);
        WaterShaderData.Instance.QueueRipple(startingPosition - rippleOffset, 0.8f, RippleShape.Square, MathF.PiOver4);

        if (progress < 1f)
        {
            return;
        }

        var velocity = -self.velocity;
        velocity.Y = -9f;

        self.velocity = velocity;

        self.ShimmerData.WaveProgress = -1f;

        // Can be fairly reasonably assumed that the bottom of the item is the top tile of the shimmer
        SpawnSpike(-48f, 16f, 0.04f);
        SpawnSpike(-16f, 32f, 0.03f);
        SpawnSpike(0f, 64f, 0.055f);
        SpawnSpike(16f, 32f, 0.03f);
        SpawnSpike(48f, 16f, 0.04f);

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
