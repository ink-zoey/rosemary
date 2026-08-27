using Daybreak.Hooks;
using Daybreak.MonoMod;
using Daybreak.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Common;
using Rosemary.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Liquid;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace Rosemary.Content.Elk;

[Autoload(Side = ModSide.Client)]
public static class ElkShimmerParticles
{
    public sealed class ShimmerSpikeHandler(int max) : UpdatingParticleHandler<ShimmerSpike>(max)
    {
        public override bool Add(ShimmerSpike particle)
        {
            if (this.Any(i => this[i].Position == particle.Position))
            {
                return false;
            }

            return base.Add(particle);
        }

        public static ShimmerSpikeHandler operator +(ShimmerSpikeHandler handler, ShimmerSpike particle)
        {
            handler.Add(particle);

            return handler;
        }
    }

    public record struct ShimmerSpike(Point Position, int XOffset, float Height, byte Style, float LifeTime, float LifeTimeIncrement) : IUpdatingParticle
    {
        public bool Update()
        {
            LifeTime += LifeTimeIncrement;

            return LifeTime <= 1f;
        }
    }

    public static ShimmerSpikeHandler Spikes { get; set; } = new(128);

    public record struct ShimmerBubble(Vector2 Position, Color Color, float Direction, byte Frame, int FrameCounter) : IUpdatingParticle
    {
        public bool Update()
        {
            FrameCounter++;

            if (FrameCounter >= 9)
            {
                Frame++;
                FrameCounter = 0;
            }
            var newPosition = Position;

            newPosition.Y -= 0.6f;
            newPosition.X += Direction * 0.6f;
            if (Collision.WetCollision(newPosition, 1, 1) && Collision.shimmer)
            {
                Position = newPosition;

                return Frame <= 4;
            }

            if (!Collision.WetCollision(Position, 1, 1) || !Collision.shimmer)
            {
                return false;
            }

            newPosition = Position;

            newPosition.X += Direction;

            Position = newPosition;

            return Frame <= 4;
        }
    }

    public static UpdatingParticleHandler<ShimmerBubble> Bubbles { get; set; } = new(256);

    public record struct ExpandingRing(Vector2 Position, float Scale, float ScaleIncrement, float LifeTime, float LifeTimeIncrement) : IUpdatingParticle
    {
        public bool Update()
        {
            Scale += ScaleIncrement;

            LifeTime += LifeTimeIncrement;

            return LifeTime <= 1f;
        }
    }

    public static UpdatingParticleHandler<ExpandingRing> Rings { get; set; } = new(64);

    [ModSystemHooks.ClearWorld]
    private static void ClearParticles()
    {
        Spikes.Clear();
        Bubbles.Clear();
        Rings.Clear();
    }

    [ModSystemHooks.PostUpdateDusts]
    private static void UpdateParticles()
    {
        Spikes.Update();
        Bubbles.Update();
        Rings.Update();
    }

    [OnLoad]
    private static void Load()
    {
        IL_LiquidRenderer.DrawShimmer += DrawShimmer_DrawSpikes;
        On_Main.DrawInfernoRings += DrawInfernoRings_DrawFlareParticles;
    }

    private static void DrawInfernoRings_DrawFlareParticles(On_Main.orig_DrawInfernoRings orig, Main self)
    {
        orig(self);

        var sb = Main.spriteBatch;

        using var _ = sb.Scope();

        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        {
            var texture = Assets.Elk.Particles.ShimmerBubble.Asset.Value;
            var origin = texture.Frame(5, 1, 0, 0).Size() * 0.5f;

            foreach (var index in Bubbles)
            {
                ref var bubble = ref Bubbles[index];

                var position = bubble.Position - Main.screenPosition;

                var frame = texture.Frame(5, 1, bubble.Frame, 0);

                var color = bubble.Color;

                color.A = 0;

                sb.Draw(texture, position, frame, color, 0f, origin, 1f, SpriteEffects.None, 0f);
            }
        }
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        {
            var texture = Assets.Elk.Particles.ExpandingCircle.Asset.Value;
            var origin = texture.Size() * 0.5f;

            var color = new Color(179, 133, 255, 40);

            foreach (var index in Rings)
            {
                ref var ring = ref Rings[index];

                var position = ring.Position - Main.screenPosition;

                var alpha = 1f - ring.LifeTime;

                sb.Draw(texture, position, null, color * alpha, 0f, origin, ring.Scale, SpriteEffects.None, 0f);
            }
        }
        sb.End();
    }

    private record struct ShimmerSpikeDrawItem(int Index, Vector2 Position);

    private static unsafe void DrawShimmer_DrawSpikes(ILContext il)
    {
        var c = new ILCursor(il);

        var spikesByPositionReference = c.AddVariable<Dictionary<Point, int>>();
        var spikesDrawCacheReference = c.AddVariable<List<ShimmerSpikeDrawItem>>();
        var useInnerFrameReference = c.AddVariable<bool>();

        var liquidRendererIndex = ParameterIndex.Invalid;
        var sourceRectangleIndex = VariableIndex.Invalid;
        var liquidCacheCurrentIndex = VariableIndex.Invalid;
        var xIndex = VariableIndex.Invalid;
        var yIndex = VariableIndex.Invalid;
        var isBackgroundDrawIndex = ParameterIndex.Invalid;

        c.EmitStaticDelegateUnsafe(
            static () =>
            {
                var dict = new Dictionary<Point, int>();

                foreach (var index in Spikes)
                {
                    dict[Spikes[index].Position] = index;
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

                if (isBackgroundDraw)
                {
                    return true;
                }

                var position =
                    tilePosition.ToWorldCoordinates(Vector2.Zero)
                  + liquidCache->LiquidOffset
                  - Main.waterTarget.Position;

                position.Y += 2f;

                spikeCache.Add(new ShimmerSpikeDrawItem(index, position));

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

                var overlayFrame = new Rectangle(0, 64, 16, 10);

                foreach (var (index, position) in spikeCache)
                {
                    var spike = Spikes[index];

                    var backFrame = texture.Frame(6, 1, spike.Style * 2, 0);
                    var frontFrame = texture.Frame(6, 1, spike.Style * 2 + 1, 0);
                    backFrame.Height = 64;
                    frontFrame.Height = 64;

                    var height = spike.Height;

                    height *=
                        MathF.Pow(1f - spike.LifeTime, 1.6f)
                      * (1f - MathF.Pow(1f - spike.LifeTime, 10f))
                      * 1.6f;

                    var dest = new Rectangle((int)position.X + spike.XOffset, (int)((position.Y - height)), 16, (int)height);

                    var backColors = GetShimmerColors(height, position, false);
                    var frontColors = GetShimmerColors(height, position, true);

                    tb.Draw(texture, dest, backFrame, backColors);
                    tb.Draw(texture, dest, frontFrame, frontColors);

                    backColors *= 0.75f;
                    frontColors *= 0.75f;

                    tb.Draw(texture, dest, backFrame, backColors);
                    tb.Draw(texture, dest, frontFrame, frontColors);

                    var tilePosition = position.ToTileCoordinates();
                    LiquidRenderer.SetShimmerVertexColors_Sparkle(ref frontColors, 0.75f, tilePosition.X, tilePosition.Y, true);

                    var overlayDest = new Rectangle((int)position.X, (int)position.Y - 1, 16, 16 - ((int)position.Y % 16));
                    tb.Draw(texture, overlayDest, overlayFrame, frontColors);
                }

                return;

                static VertexColors GetShimmerColors(float height, Vector2 position, bool useSparkleColor)
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

                    return colors;
                }
            }
        );
    }
}
