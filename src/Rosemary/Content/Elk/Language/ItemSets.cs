using Daybreak.Common.CIL;
using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using static System.Net.Mime.MediaTypeNames;
using static Terraria.ModLoader.PlayerDrawLayer;

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

        On_PopupText.NewText_PopupTextContext_Item_Vector2_int_bool_bool += NewText_UsesElkName_UpdatePopupTextItems;
        On_PopupText.ResetText += ResetText_UsesElkName;

        On_PopupText.GetTextHitbox += GetTextHitbox_UsesElkName;
        IL_PopupText.Update += _ => { };

        IL_PopupText.NewText_PopupTextContext_Item_Vector2_int_bool_bool += NewText_UsesElkName;

        IL_PopupText.DrawItemTextPopups += DrawItemTextPopups_UsesElkName;
    }

#region UsesElkName
    private const float elk_name_tooltip_scale = 1f;
    private const float elk_name_popup_scale = 1f;

    private static Item? nonTooltipHoverItem;

    private static Item?[] popupTextItems = new Item?[PopupText.popupText.Length];

    private static void DrawItemTextPopups_UsesElkName(ILContext il)
    {
        var c = new ILCursor(il);

        var popupTextIndex = -1;
        var scaleMultiplierIndex = -1;
        var magicAlphaMultiplierIndex = -1;

        var colorIndex = -1;

        ILLabel? contLoopTarget = null;

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdloc(out popupTextIndex),
            i => i.MatchLdfld<PopupText>(nameof(PopupText.active)),
            i => i.MatchBrfalse(out contLoopTarget)
        );

        Debug.Assert(contLoopTarget is not null);

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdfld<PopupText>(nameof(PopupText.scale)),
            i => i.MatchLdloc(out _),
            i => i.MatchDiv(),
            i => i.MatchStloc(out scaleMultiplierIndex)
        );

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdcR4(1f),
            i => i.MatchLdloc(out _),
            i => i.MatchCallvirt<string>($"get_{nameof(string.Length)}")
        );

        c.GotoNext(
            i => i.MatchSwitch(out _)
        );

        c.GotoPrev(
            MoveType.After,
            i => i.MatchDiv(),
            i => i.MatchStloc(out _)
        );

        c.FindNext(
            out _,
            i => i.MatchBr(out _),
            i => i.MatchLdloc(out colorIndex),

            i => i.MatchLdfld<PopupText>(nameof(PopupText.alpha)),
            i => i.MatchLdloc(out magicAlphaMultiplierIndex)
        );

        c.EmitLdloc(popupTextIndex);
        c.EmitLdloc(colorIndex);
        c.EmitLdloc(scaleMultiplierIndex);
        c.EmitLdloc(magicAlphaMultiplierIndex);
        c.EmitDelegate(
            static (PopupText popupText, Color origColor, float scaleMultiplier, float magic) =>
            {
                var index = PopupText.popupText.IndexOf(popupText);

                var item = popupTextItems[index];

                if (item is null || usesElkName[item.type] is not { } phrase)
                {
                    return false;
                }

                var sb = Main.spriteBatch;

                var size = phrase.Measure(1f);

                var multiplier = (float)Main.mouseTextColor / byte.MaxValue;

                var gradient = popupText.color * scaleMultiplier * popupText.alpha * magic;
                var white = Color.White * multiplier;
                white.A = byte.MaxValue;

                white *= popupText.alpha * magic;

                var fade = 1 - (float)Utils.EaseOutCirc(Utils.Remap(popupText.framesSinceSpawn, 0f, 40, 0f, 1f));

                gradient = Color.Lerp(gradient, Color.White, fade);

                white = Color.Lerp(white, Color.White, fade);

                var outlineGradient = origColor;
                var black = Color.Black;

                {
                    var outlineAlpha = popupText.color.A * scaleMultiplier * popupText.alpha;

                    outlineGradient.A = (byte)MathHelper.Lerp(60f, 127f, Utils.GetLerpValue(0f, 255f, outlineAlpha, clamped: true));

                    var outlineGradientAlt = new Color(0, 0, 0, (int)outlineAlpha);

                    outlineGradient = Color.Lerp(outlineGradient, outlineGradientAlt, 0.25f);
                    black *= outlineAlpha / byte.MaxValue;
                }

                // The gradient looks particularly ugly with the colors given in this case.
                if (popupText.context == PopupTextContext.ItemReforge_Best)
                {
                    white = gradient;
                    black = outlineGradient;
                }

                var scale = elk_name_popup_scale * popupText.scale;

                var position = popupText.position - Main.screenPosition + (size * elk_name_popup_scale * 0.5f);

                // TODO: Maybe account for rotation? PopupText doesn't use it however so it should be fine.
                sb.DrawItemNamePhrase(phrase, item, position, gradient, white, outlineGradient, black, scale, size * 0.5f);

                return true;
            }
        );

        c.EmitBrtrue(contLoopTarget);
    }

    private static void NewText_UsesElkName(ILContext il)
    {
        var c = new ILCursor(il);

        var itemIndex = -1; // arg

        c.GotoNext(
            i => i.MatchLdarg(out itemIndex),
            i => i.MatchCallvirt<Item>($"get_{nameof(Item.Name)}")
        );

        while (c.TryGotoNext(
                   MoveType.After,
                   i => i.MatchCallvirt<DynamicSpriteFont>(nameof(DynamicSpriteFont.MeasureString))
               ))
        {
            c.EmitLdarg(itemIndex);

            c.EmitDelegate(
                static (Vector2 size, Item item) =>
                {
                    if (usesElkName[item.type] is not { } phrase)
                    {
                        return size;
                    }

                    return phrase.MeasureWithStack(elk_name_popup_scale, item.stack);
                }
            );
        }
    }

    private static Vector2 GetTextHitbox_UsesElkName(On_PopupText.orig_GetTextHitbox orig, PopupText self)
    {
        var index = PopupText.popupText.IndexOf(self);

        var item = popupTextItems[index];

        if (item is null || usesElkName[item.type] is not { } phrase)
        {
            return orig(self);
        }

        return phrase.MeasureWithStack(elk_name_popup_scale, item.stack) * self.scale;
    }

    private static void ResetText_UsesElkName(On_PopupText.orig_ResetText orig, PopupText text)
    {
        orig(text);

        var index = PopupText.popupText.IndexOf(text);

        popupTextItems[index] = null;
    }

    private static int NewText_UsesElkName_UpdatePopupTextItems(On_PopupText.orig_NewText_PopupTextContext_Item_Vector2_int_bool_bool orig, PopupTextContext context, Item newItem, Vector2 position, int stack, bool noStack, bool longText)
    {
        var index = orig(context, newItem, position, stack, noStack, longText);

        if (index <= -1)
        {
            return index;
        }

        // Should probably never happen.
        if (popupTextItems.Length != PopupText.popupText.Length)
        {
            Array.Resize(ref popupTextItems, PopupText.popupText.Length);
        }

        popupTextItems[index] = newItem.Clone();

        return index;
    }

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

                return phrase.MeasureWithStack(elk_name_tooltip_scale, item.stack);
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

                Main.spriteBatch.DrawItemNamePhrase(phrase, item, new Vector2(x + 6f, y + 8f));

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

                var transform = ss.TransformMatrix;

                var topLeft = new Vector2(0, 20);

                var bottomLeft = topLeft;
                bottomLeft.Y += MathF.Min(150f, size.Y - 20);

                topLeft = topLeft.Transform(transform);
                bottomLeft = bottomLeft.Transform(transform);

                hotbarShader.Parameters.GradientTop = topLeft.Y;
                hotbarShader.Parameters.GradientHeight = bottomLeft.Y - topLeft.Y;

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

                var phraseSize = phrase.MeasureWithStack(elk_name_tooltip_scale, Main.HoverItem.stack);

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

        var size = phrase.MeasureWithStack(elk_name_tooltip_scale, item.stack);

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

        sb.DrawItemNamePhrase(phrase, item, position);
    }

    private static void DrawItemNamePhrase(this SpriteBatch sb, ElkPhrase phrase, Item item, Vector2 position, bool showPrefix = true)
    {
        var multiplier = (float)Main.mouseTextColor / byte.MaxValue;

        var gradient = GetRarityColor();

        var white = Color.White * multiplier;

        white.A = byte.MaxValue;

        sb.DrawItemNamePhrase(phrase, item, position, gradient, white, Color.Black, Color.Black, elk_name_tooltip_scale, Vector2.Zero, showPrefix);

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
    }

    private static void DrawItemNamePhrase(this SpriteBatch sb, ElkPhrase phrase, Item item, Vector2 position, Color gradient, Color white, Color outlineGradient, Color black, float scale, Vector2 origin = default, bool showPrefix = true)
    {
        var rarityShader = Assets.Elk.Language.RarityGradient.CreateRarityGradientShader();

        var size = phrase.Measure(scale);

        sb.End(out var ss);

        const float padding = 20f;

        var transform = ss.TransformMatrix;

        var topLeft = position - (origin * scale);
        topLeft.Y -= padding;

        var bottomLeft = topLeft;
        bottomLeft.Y += size.Y + padding;

        topLeft = topLeft.Transform(transform);
        bottomLeft = bottomLeft.Transform(transform);

        rarityShader.Parameters.GradientTop = topLeft.Y;
        rarityShader.Parameters.GradientHeight = bottomLeft.Y - topLeft.Y;

        rarityShader.Parameters.GradientColor = outlineGradient.ToVector4();

        rarityShader.Apply();

        sb.Begin(ss with { CustomEffect = rarityShader.Shader });
        {
            sb.DrawPhraseOutline(phrase, position, black, scale, origin, spread: 1.5f, directions: 8);
        }
        sb.End();

        rarityShader.Parameters.GradientColor = gradient.ToVector4();

        rarityShader.Apply();

        sb.Begin(ss with { CustomEffect = rarityShader.Shader });
        {
            sb.DrawPhrase(phrase, position, white, scale, origin);
        }
        sb.Restart(in ss);

        DrawStack();

        if (showPrefix)
        {
            DrawPrefix();
        }

        return;

        void DrawStack()
        {
            if (item.stack <= 1)
            {
                return;
            }

            var font = FontAssets.MouseText.Value;

            var stackText = $"({item.stack})";

            var stackPosition = new Vector2(position.X + (size.X * 0.5f), position.Y + size.Y);
            stackPosition -= origin * scale;

            var stackSize = font.MeasureString(stackText);

            var stackScale = size.X / stackSize.X;
            stackScale = MathF.Min(1f, stackScale);

            var stackOrigin = stackSize * new Vector2(0.5f, 0f);

            ChatManager.DrawColorCodedStringWithShadow(
                sb,
                font,
                stackText,
                stackPosition,
                white,
                black,
                0f,
                stackOrigin,
                new Vector2(stackScale),
                maxWidth: 999f
            );
        }

        void DrawPrefix()
        {
            if (item.prefix == 0)
            {
                return;
            }

            var font = FontAssets.MouseText.Value;

            var prefixText = Lang.prefix[item.prefix].Value;

            var lastCharacterHeight = phrase[^1].Height - phrase[^1].Position.Y;

            var prefixPosition = new Vector2(position.X + 10f, position.Y + size.Y - (lastCharacterHeight * 0.5f));
            prefixPosition -= origin * scale;

            var prefixRotation = -MathHelper.PiOver2;

            var prefixSize = font.MeasureString(prefixText);
            var prefixScale = 0.9f * scale;

            var prefixOrigin = prefixSize * new Vector2(0.5f, 1f);

            ChatManager.DrawColorCodedStringWithShadow(
                sb,
                font,
                prefixText,
                prefixPosition,
                white,
                black,
                prefixRotation,
                prefixOrigin,
                new Vector2(prefixScale),
                maxWidth: 999f
            );
        }
    }

    private static Vector2 MeasureWithStack(this ElkPhrase phrase, float scale, int stack)
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
