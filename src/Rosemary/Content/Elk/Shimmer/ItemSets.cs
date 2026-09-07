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
}
