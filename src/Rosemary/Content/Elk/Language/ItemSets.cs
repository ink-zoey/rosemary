using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content;

public static class ElkLangItemSets
{
    private static ElkPhrase?[] usesElkName = [];

    private static Mod Mod => ModContent.GetInstance<ModImpl>();

    [ModSystemHooks.ResizeArrays]
    private static void ResizeArrays()
    {
        usesElkName = CreateSet<ElkPhrase?>(nameof(usesElkName), null);

        return;

        static T[] CreateSet<T>(string name, T defaultState)
        {
            return ItemID.Sets.Factory.CreateNamedSet(Mod, name)
                         .RegisterCustomSet(defaultState);
        }
    }

    extension(ItemID.Sets)
    {
        public static ElkPhrase?[] UsesElkName => usesElkName;
    }

    [OnLoad]
    private static void Load()
    {
        IL_Main.MouseText_DrawItemTooltip += MouseText_DrawItemTooltip_UsesElkName;
    }

    private const float elk_name_tooltip_scale = 1f;

    private static void MouseText_DrawItemTooltip_UsesElkName(ILContext il)
    {
        var c = new ILCursor(il);

        var sizeIndex = -1;

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdarg(out _),
            i => i.MatchLdsfld<Main>(nameof(Main.toolTipDistance))
        );

        c.MoveAfterLabels();

        c.TryFindPrev(
            out _,
            i => i.MatchLdloca(out sizeIndex),
            i => i.MatchLdflda<Vector2>(nameof(Vector2.Y))
        );

        c.EmitLdloca(sizeIndex);

        c.EmitDelegate(
            static (ref Vector2 size) =>
            {
                if (usesElkName[Main.HoverItem.type] is not { } phrase)
                {
                    return;
                }

                var elkSize = phrase.Measure(elk_name_tooltip_scale);

                size.X += elkSize.X + 4f;
                size.Y = MathF.Max(size.Y, elkSize.Y);
            }
        );
    }

    [GlobalItemHooks.ModifyTooltips]
    private static void ModifyTooltips_UsesElkName(Item item, List<TooltipLine> tooltips)
    {
        if (usesElkName[Main.HoverItem.type] is null)
        {
            return;
        }

        tooltips.Find(line => line is { Mod: "Terraria", Name: "ItemName" })?.Hide();
    }

    // Why doesn't PostDrawTooltip provide an x and y?
    private static Vector2 usesElkNameTopLeft;

    [GlobalItemHooks.PreDrawTooltip]
    private static bool PreDrawTooltip_UsesElkName(Item item, ReadOnlyCollection<TooltipLine> lines, ref int x, ref int y)
    {
        if (usesElkName[Main.HoverItem.type] is not { } phrase)
        {
            return true;
        }

        var elkSize = phrase.Measure(elk_name_tooltip_scale);

        usesElkNameTopLeft = new Vector2(x, y);

        x += (int)(elkSize.X + 4f);

        return true;
    }

    [GlobalItemHooks.PostDrawTooltip]
    private static void PostDrawTooltip_UsesElkName(Item item, ReadOnlyCollection<DrawableTooltipLine> lines)
    {
        if (usesElkName[Main.HoverItem.type] is not { } phrase)
        {
            return;
        }

        var sb = Main.spriteBatch;

        var position = usesElkNameTopLeft;
        position.X -= 6f;

        var multiplier = (float)Main.mouseTextColor / byte.MaxValue;

        var color = ItemRarity.GetColor(item.rare);

        if (item.expert || item.rare == ItemRarityID.Expert)
        {
            color = Main.DiscoColor;
        }
        if (item.master || item.rare == ItemRarityID.Master)
        {
            color = new Color(255, (byte)(Main.masterColor * 200), 0);
        }

        // For whatever reason the mouseTextColor multiplier is baked into
        // ItemRarity.GetColor, but only for the standard white rarity.
        if (ItemRarity._rarities.ContainsKey(item.rare))
        {
            color *= multiplier;
        }

        color.A = byte.MaxValue;

        sb.DrawPhraseWithOutline(phrase, position, color, Color.Black, elk_name_tooltip_scale, Vector2.Zero, spread: 1.5f, directions: 8);
    }
}
