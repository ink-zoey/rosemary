using GoldMeridian.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Common;
using Rosemary.Core;
using System;
using Terraria;
using Terraria.GameContent.Liquid;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content.Elk;

public static partial class ElkShimmerItemSets
{
    [ExtensionDataFor<WorldItem>("ShimmerData")]
    internal sealed class ShimmerReactionData
    {
        public required float WaveProgress { get; set; }

        public required float SubSurfaceProgress { get; set; }

        public required bool LoopingSound { get; set; }
    }

    private static bool[] violentShimmerReaction = [];

    private static bool[] solidShimmerReaction = [];

    private static Mod Mod => ModContent.GetInstance<ModImpl>();

    [ModSystemHooks.ResizeArrays]
    private static void ResizeArrays()
    {
        violentShimmerReaction = CreateSet(nameof(violentShimmerReaction), false);
        solidShimmerReaction = CreateSet(nameof(solidShimmerReaction), false);

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

        public static bool[] SolidShimmerReaction => solidShimmerReaction;
    }

    [OnLoad]
    private static void Load_Misc()
    {
        On_LiquidRenderer.DrawShimmer += DrawShimmer_Mesmerizers;
        IL_Main.DrawItem += DrawItem_ShimmerRadiance;
        On_WorldItem.UpdateItem_VisualEffects += UpdateItem_VisualEffects_Radiance;
    }

    private static void UpdateItem_VisualEffects_Radiance(On_WorldItem.orig_UpdateItem_VisualEffects orig, WorldItem self)
    {
        orig(self);

        if (!ShimmerRadianceInfo(self, out var interpolator, out _))
        {
            return;
        }

        Lighting.AddLight(self.Center, Color.White * interpolator);
    }

    private static void DrawItem_ShimmerRadiance(ILContext il)
    {
        var c = new ILCursor(il);

        var itemIndex = ParameterIndex.Invalid;
        var colorIndex = VariableIndex.Invalid;

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdarg(out itemIndex),
            i => i.MatchLdloc(out int _),
            i => i.MatchCallvirt<WorldItem>(nameof(WorldItem.GetAlpha)),
            i => i.MatchStloc(out colorIndex)
        );

        c.EmitLdarg(itemIndex);
        c.EmitLdloca(colorIndex);
        c.EmitDelegate(
            static (WorldItem item, ref Color color) =>
            {
                if (!ShimmerRadianceInfo(item, out var interpolator, out _))
                {
                    return;
                }

                color = Color.Lerp(color, Color.White, interpolator);
            }
        );
    }

    [GlobalItemHooks.PostDrawInWorld]
    private static void PostDrawInWorld_ShimmerRadiance(WorldItem item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
    {
        if (!ShimmerRadianceInfo(item, out var interpolator, out var amplitude))
        {
            return;
        }

        Main.instance.DrawItem_GetBasics(item.inner, whoAmI, out var texture, out var frame, out _);

        var origin = frame.Size() * 0.5f;

        var off = new Vector2((item.width * 0.5f) - origin.X, item.height - frame.Height);

        var position = (item.position + origin + off) - Main.screenPosition;

        var color = alphaColor;
        color.A = 0;

        color *= interpolator;

        const float freq = 7f;

        var time = Main.GlobalTimeWrappedHourly * freq;

        var wave = ((time % 1f) - 0.5f) * 2f;
        wave = (MathF.Abs(wave) - 0.5f) * 2f;

        scale *= 1f + (wave * amplitude * interpolator);

        spriteBatch.Draw(texture, position, frame, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    private static bool ShimmerRadianceInfo(WorldItem item, out float interpolator, out float amplitude)
    {
        var data = item.ShimmerData;

        var reactant = item.shimmerWet
                    && ItemID.Sets.ViolentShimmerReaction[item.type]
                    && data is not null
                    && (data.WaveProgress > 0f
                     || data.SubSurfaceProgress > 0f);

        var result = ItemID.Sets.SolidShimmerReaction[item.type];

        interpolator = 1f;
        amplitude = 0f;

        if (ItemID.Sets.ViolentShimmerReaction[item.type] && data is not null)
        {
            interpolator = 1f - MathF.Pow(1f - data.WaveProgress, 3f);
            interpolator = MathF.Max(interpolator, 1f - MathF.Pow(1f - data.SubSurfaceProgress, 12f));

            amplitude = 0.5f * (1f - MathF.Pow(data.SubSurfaceProgress, 12f));
        }

        return reactant || result;
    }

    private static void DrawShimmer_Mesmerizers(On_LiquidRenderer.orig_DrawShimmer orig, LiquidRenderer self, SpriteBatch sb, Vector2 drawOffset, bool isBackgroundDraw)
    {
        orig(self, sb, drawOffset, isBackgroundDraw);

        if (isBackgroundDraw)
        {
            return;
        }

        const float spike_count = 8;

        var texture = Assets.Elk.Particles.ExpandingCircle.Asset.Value;
        var origin = texture.Size() * 0.5f;

        using var _ = sb.Scope();

        var mesmerShaderizer = Assets.Elk.Shimmer.Mesmerizer.CreateMesmerizerShader();

        using var lease = ScreenspaceTargetProvider.Shared.Create(Main.graphics.GraphicsDevice, (_, _, targetWidth, targetHeight) => (targetWidth, targetHeight));

        using (lease.Scope(clearColor: Color.Transparent))
        {
            mesmerShaderizer.Parameters.SpikeCount = spike_count;
            mesmerShaderizer.Parameters.Time = (float)Main.timeForVisualEffects * 0.013f;

            mesmerShaderizer.Apply();

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, mesmerShaderizer.Shader, Matrix.Identity);
            {
                DrawMesmerizers();
            }
            sb.End();
        }

        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaMask, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);
        {
            var shimmerMesmerizer = Assets.Elk.Shimmer.MesmerizerShimmerColors.CreateMesmerizerShimmerColorsShader();

            shimmerMesmerizer.Parameters.Texture = new HlslSampler2D
            {
                Texture = lease.Target,
                Sampler = SamplerState.PointClamp,
            };

            shimmerMesmerizer.Parameters.Time = (float)Main.timeForVisualEffects;
            shimmerMesmerizer.Parameters.TargetPosition = Main.waterTarget.Position;

            shimmerMesmerizer.Apply();

            sb.Draw(lease.Target, Vector2.Zero, Color.White);
        }
        sb.End();

        sb.Begin(SpriteSortMode.Immediate, BlendState.InverseMultiplicative, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);
        {
            var inlineMesmerizer = Assets.Elk.Shimmer.MesmerizerInset.CreateMesmerizerInsetShader();

            inlineMesmerizer.Parameters.Texture = new HlslSampler2D
            {
                Texture = lease.Target,
                Sampler = SamplerState.PointClamp,
            };

            inlineMesmerizer.Parameters.TargetPosition = Main.waterTarget.Position;

            inlineMesmerizer.Apply();

            sb.Draw(lease.Target, Vector2.Zero, Color.White);
        }
        sb.End();

        return;

        void DrawMesmerizers()
        {
            foreach (var item in Main.ActiveItems)
            {
                if (!item.shimmerWet
                 || !MesmerizerInfo(item, out var scale, out var alpha))
                {
                    continue;
                }

                var curPosition = FindShimmerSurface(item, 16);

                var dist = curPosition == item.Bottom
                    ? 1f
                    : (1f - MathF.Pow(1f - MathF.Saturate(MathF.Abs(item.Center.Y - curPosition.Y) / 32f), 2f));

                Main.instance.DrawItem_GetBasics(item.inner, item.whoAmI, out var _, out var frame, out var _);

                var itemOrigin = frame.Size() * 0.5f;

                var topLeft = new Vector2((item.width * 0.5f) - itemOrigin.X, item.height - frame.Height);
                var center = item.position + itemOrigin + topLeft;

                center -= Main.waterTarget.Position;

                var time = Main.GlobalTimeWrappedHourly * 3f;

                var rotation = (1 - MathF.Cos(MathF.PI * (time % 1f))) * 0.5f + MathF.Floor(time);
                rotation *= MathF.Tau / (spike_count - 0.5f);

                var size = scale * dist * 0.15f;

                var color = Color.White * alpha;

                sb.Draw(texture, center, null, color, rotation, origin, size, SpriteEffects.None, 0f);
            }
        }

        static bool MesmerizerInfo(WorldItem item, out float scaleMultiplier, out float noiseMultiplier)
        {
            var reactant = ItemID.Sets.ViolentShimmerReaction[item.type] && item.ShimmerData is { SubSurfaceProgress: > 0f };
            var result = ItemID.Sets.SolidShimmerReaction[item.type];

            const float low_noise = 0.25f;

            scaleMultiplier = 1f;
            noiseMultiplier = low_noise;

            if (ItemID.Sets.ViolentShimmerReaction[item.type] && item.ShimmerData is { } data)
            {
                scaleMultiplier = 1f - MathF.Pow(1f - data.SubSurfaceProgress, 12f);

                noiseMultiplier = scaleMultiplier;
                noiseMultiplier *= Utils.Remap(1f - MathF.Pow(data.SubSurfaceProgress, 23f), 0f, 1f, low_noise, 1f);
            }

            return reactant || result;
        }
    }
}
