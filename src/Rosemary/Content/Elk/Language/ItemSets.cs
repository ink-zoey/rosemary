using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using static Daybreak.Common.Features.Hooks.ModifyItemDrawBasics;

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
        IL_Main.GUIHotbarDrawInner += GUIHotbarDrawInner_UsesElkName;
        IL_Main.DrawMouseOver += DrawMouseOver_UsesElkName;

        MonoModHooks.Add(
            typeof(Main).GetMethod(
                nameof(Main.MouseTextInner),
                BindingFlags.Instance | BindingFlags.NonPublic
            ),
            MouseTextInner_UsesElkName_Reset
        );
        IL_Main.MouseTextInner += MouseTextInner_UsesElkName;
    }

#region UsesElkName
    private const float elk_name_tooltip_scale = 1f;

    private static Item? nonTooltipHoverItem;

    private static void MouseTextInner_UsesElkName(ILContext il)
    {
        var c = new ILCursor(il);

        var bigXIndex = -1;
        var bigYIndex = -1;

        var jumpDrawStringTarget = c.DefineLabel();

        c.GotoNext(
            MoveType.After,
            i => i.MatchCall(typeof(ChatManager), nameof(ChatManager.GetStringSize))
        );

        c.EmitDelegate(
            static (Vector2 originalSize) =>
            {
                var item = nonTooltipHoverItem;

                if (item is null || usesElkName[item.type] is not { } phrase)
                {
                    return originalSize;
                }

                return phrase.MeasurePhraseWithStack(elk_name_tooltip_scale, item.stack);
            }
        );

        c.GotoNext(
            i => i.MatchLdfld<Main.MouseTextCache>(nameof(Main.MouseTextCache.buffTooltip)),
            i => i.MatchLdloca(out bigXIndex),
            i => i.MatchLdloca(out bigYIndex)
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow))
        );

        c.MarkLabel(jumpDrawStringTarget);

        c.GotoPrev(
            MoveType.Before,
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch))
        );

        c.MoveAfterLabels();

        c.EmitLdloc(bigXIndex);
        c.EmitLdloc(bigYIndex);

        c.EmitDelegate(
            static (int x, int y) =>
            {
                var item = nonTooltipHoverItem;

                if (item is null || usesElkName[item.type] is not { } phrase)
                {
                    return false;
                }

                Main.spriteBatch.DrawPhraseWithRarityAndStack(phrase, item, new Vector2(x + 6f, y + 8f));

                return true;
            }
        );

        c.EmitBrtrue(jumpDrawStringTarget);
    }

    private static void MouseTextInner_UsesElkName_Reset(Action<Main, Main.MouseTextCache> orig, Main self, Main.MouseTextCache info)
    {
        orig(self, info);

        nonTooltipHoverItem = null;
    }

    private static void DrawMouseOver_UsesElkName(ILContext il)
    {
        var c = new ILCursor(il);

        var worldItemIndexIndex = -1;

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.item)),
            i => i.MatchLdloc(out worldItemIndexIndex),
            i => i.MatchLdelemRef(),
            i => i.MatchCallvirt<WorldItem>($"get_{nameof(WorldItem.master)}")
        );

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdarg(out _)
        );

        c.MoveAfterLabels();

        c.EmitLdloc(worldItemIndexIndex);

        c.EmitDelegate(
            static (int i) =>
            {
                nonTooltipHoverItem = Main.item[i].inner;
            }
        );
    }

    private static void GUIHotbarDrawInner_UsesElkName(ILContext il)
    {
        var c = new ILCursor(il);

        var magicNumber = -1f;

        var hotbarIndexIndex = -1;

        var jumpDrawStringTarget = c.DefineLabel();

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdloca(out _),
            i => i.MatchLdcR4(out magicNumber),
            i => i.MatchLdsfld(typeof(FontAssets), nameof(FontAssets.MouseText))
        );

        c.MoveAfterLabels();

        c.EmitLdcR4(magicNumber);

        c.EmitDelegate(
            static (float centerX) =>
            {
                var player = Main.LocalPlayer;

                var item = player.inventory[player.selectedItem];

                if (usesElkName[item.type] is not { } phrase)
                {
                    return false;
                }

                var sb = Main.spriteBatch;

                var hotbarShader = Assets.Elk.Language.HotbarGradient.CreateHotbarGradientShader();

                var size = phrase.Measure(elk_name_tooltip_scale);

                var origin = new Vector2(size.X * 0.5f, 0f);

                var position = new Vector2(centerX, 0f);

                var multiplier = (float)Main.mouseTextColor / byte.MaxValue;

                sb.End(out var ss);

                hotbarShader.Parameters.GradientTop = 20;
                hotbarShader.Parameters.GradientHeight = MathF.Min(150f, size.Y - 20);

                hotbarShader.Apply();

                sb.Begin(ss with { CustomEffect = hotbarShader.Shader });
                {
                    var color = Color.White * multiplier;

                    sb.DrawPhrase(phrase, position, color, elk_name_tooltip_scale, origin);
                }
                sb.Restart(in ss);

                return true;
            }
        );

        c.EmitBrtrue(jumpDrawStringTarget);

        c.GotoNext(
            MoveType.After,
            i => i.MatchCall(typeof(DynamicSpriteFontExtensionMethods), nameof(DynamicSpriteFontExtensionMethods.DrawString))
        );

        c.MarkLabel(jumpDrawStringTarget);

        c.GotoNext(
            i => i.MatchLdsfld<Main>(nameof(Main.hotbarScale)),
            i => i.MatchLdloc(out hotbarIndexIndex),
            i => i.MatchLdelemR4()
        );

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdsfld<Main>(nameof(Main.player)),
            i => i.MatchLdsfld<Main>(nameof(Main.myPlayer)),
            i => i.MatchLdelemRef(),
            i => i.MatchLdcI4(1),
            i => i.MatchStfld<Player>(nameof(Player.mouseInterface))
        );

        c.MoveAfterLabels();

        c.EmitLdloc(hotbarIndexIndex);
        c.EmitDelegate(
            static (int index) =>
            {
                nonTooltipHoverItem = Main.LocalPlayer.inventory[index];
            }
        );
    }

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

                var phraseSize = phrase.MeasurePhraseWithStack(elk_name_tooltip_scale, Main.HoverItem.stack);

                size.X += phraseSize.X + 4f;
                size.Y = MathF.Max(size.Y, phraseSize.Y);
            }
        );
    }

    [GlobalItemHooks.ModifyTooltips]
    private static void ModifyTooltips_UsesElkName(Item item, List<TooltipLine> tooltips)
    {
        if (usesElkName[item.type] is null)
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
        if (usesElkName[item.type] is not { } phrase)
        {
            return true;
        }

        var size = phrase.MeasurePhraseWithStack(elk_name_tooltip_scale, item.stack);

        usesElkNameTopLeft = new Vector2(x, y);

        x += (int)(size.X + 4f);

        return true;
    }

    [GlobalItemHooks.PostDrawTooltip]
    private static void PostDrawTooltip_UsesElkName(Item item, ReadOnlyCollection<DrawableTooltipLine> lines)
    {
        if (usesElkName[item.type] is not { } phrase)
        {
            return;
        }

        var sb = Main.spriteBatch;

        var position = usesElkNameTopLeft;
        position.X -= 6f;

        sb.DrawPhraseWithRarityAndStack(phrase, item, position);
    }

    private static void DrawPhraseWithRarityAndStack(this SpriteBatch sb, ElkPhrase phrase, Item item, Vector2 position)
    {
        var rarityShader = Assets.Elk.Language.RarityGradient.CreateRarityGradientShader();

        var size = phrase.Measure(elk_name_tooltip_scale);

        var multiplier = (float)Main.mouseTextColor / byte.MaxValue;

        var rarityColor = GetRarityColor();

        var color = Color.White * multiplier;

        color.A = byte.MaxValue;

        sb.DrawPhraseOutline(phrase, position, Color.Black, elk_name_tooltip_scale, Vector2.Zero, spread: 1.5f, directions: 8);

        sb.End(out var ss);

        const float padding = 20f;

        rarityShader.Parameters.GradientTop = position.Y - padding;
        rarityShader.Parameters.GradientHeight = size.Y + padding;

        rarityShader.Parameters.GradientColor = rarityColor.ToVector4();

        rarityShader.Apply();

        sb.Begin(ss with { CustomEffect = rarityShader.Shader });
        {
            sb.DrawPhrase(phrase, position, color, elk_name_tooltip_scale, Vector2.Zero);
        }
        sb.Restart(in ss);

        DrawStack();

        return;

        Color GetRarityColor()
        {
            var col = ItemRarity.GetColor(item.rare);

            if (item.expert || item.rare == ItemRarityID.Expert)
            {
                col = Main.DiscoColor;
            }

            if (item.master || item.rare == ItemRarityID.Master)
            {
                col = new Color(255, (byte)(Main.masterColor * 200), 0);
            }

            // For whatever reason the mouseTextColor multiplier is baked into
            // ItemRarity.GetColor, but only for the standard white rarity.
            if (ItemRarity._rarities.ContainsKey(item.rare))
            {
                col *= multiplier;
            }

            col.A = byte.MaxValue;

            return col;
        }

        void DrawStack()
        {
            if (item.stack <= 1)
            {
                return;
            }

            var font = FontAssets.MouseText.Value;

            var stackText = $"({item.stack})";

            var stackPosition = new Vector2(position.X + (size.X * 0.5f), position.Y + size.Y);

            var stackSize = font.MeasureString(stackText);

            var stackScale = size.X / stackSize.X;
            stackScale = MathF.Min(1f, stackScale);

            var stackOrigin = stackSize * new Vector2(0.5f, 0f);

            ChatManager.DrawColorCodedStringWithShadow(
                sb,
                font,
                stackText,
                stackPosition,
                color,
                Color.Black,
                0f,
                stackOrigin,
                new Vector2(stackScale),
                maxWidth: 999f
            );
        }
    }

    private static Vector2 MeasurePhraseWithStack(this ElkPhrase phrase, float scale, int stack)
    {
        var size = phrase.Measure(elk_name_tooltip_scale);

        if (stack <= 1)
        {
            return size * scale;
        }

        var font = FontAssets.MouseText.Value;

        var stackText = $"({stack})";

        var stackSize = font.MeasureString(stackText);

        var origStackWidth = stackSize.X;

        stackSize /= stackSize.X;
        stackSize *= MathF.Min(origStackWidth, size.X);

        size.Y += stackSize.Y;

        return size * scale;
    }

#endregion
}
