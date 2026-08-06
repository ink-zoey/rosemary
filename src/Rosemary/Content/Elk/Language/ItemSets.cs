using Daybreak.Hooks;
using Daybreak.MonoMod;
using Daybreak.Rendering;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Graphics;
using Rosemary.Common;
using Rosemary.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Liquid;
using Terraria.GameContent.UI;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace Rosemary.Content.Elk;

public static class ElkLangItemSets
{
    private static ElkPhrase?[] usesElkName = [];

    private static bool[] violentShimmerReaction = [];

    private static Mod Mod => ModContent.GetInstance<ModImpl>();

    [ModSystemHooks.ResizeArrays]
    private static void ResizeArrays()
    {
        usesElkName = CreateSet<ElkPhrase?>(nameof(usesElkName), null);
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
        ///     If not <see langword="null"/> for a given item, the item will use the given Elklang
        ///     phrase inplace of all covered cases of the item's name.<br/>
        ///     Additionally, reforging will be slower and have extra spark particles as to feel more foreign.
        /// </summary>
        public static ElkPhrase?[] UsesElkName => usesElkName;

        /// <summary>
        ///     TODO
        /// </summary>
        public static bool[] ViolentShimmerReaction => violentShimmerReaction;
    }

#region UsesElkName
    [OnLoad]
    private static void Load_UsesElkName()
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
        IL_PopupText.Update += Update_UsesElkName;

        IL_PopupText.NewText_PopupTextContext_Item_Vector2_int_bool_bool += NewText_UsesElkName;

        IL_PopupText.DrawItemTextPopups += DrawItemTextPopups_UsesElkName;

        IL_PopupText.EmitFancyFlashDust += EmitFancyFlashDust_UsesElkName;

        IL_Main.ReforgeItemInReforgeSlot += ReforgeItemInReforgeSlot_UsesElkName;
    }

    private const float elk_name_tooltip_scale = 1f;
    private const float elk_name_popup_scale = 1f;

    private static Item? nonTooltipHoverItem;

    private static Item?[] popupTextItems = new Item?[PopupText.popupText.Length];

    private static void ReforgeItemInReforgeSlot_UsesElkName(ILContext il)
    {
        var c = new ILCursor(il);

        var bestReforgeIndex = -1;

        var jumpRetTarget = c.DefineLabel();

        c.GotoNext(
            i => i.MatchCall<PopupText>(nameof(PopupText.NewText))
        );

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdloc(out bestReforgeIndex),
            i => i.MatchBrfalse(out _)
        );

        c.EmitLdloc(bestReforgeIndex);

        c.EmitDelegate(
            static (bool rolledPrefixIsTopTier) =>
            {
                const float y_offset = -16f;

                var item = Main.reforgeItem;

                if (item is null || usesElkName[item.type] is not { } phrase)
                {
                    return false;
                }

                var player = Main.LocalPlayer;

                var size = phrase.Measure(elk_name_popup_scale);

                var offset = y_offset - (size.Y * 0.5f);
                var position = player.MountedCenter + new Vector2(0f, offset * player.gravDir);

                var ySpeed = size.Y * 0.06f;

                if (rolledPrefixIsTopTier)
                {
                    SoundEngine.PlaySound(in SoundID.BestReforge);
                    Main.reforgeCooldown = 110;

                    SpawnBestSparks();
                }
                else
                {
                    SoundEngine.PlaySound(Assets.Elk.Reforge.Asset with { PitchRange = (-0.3f, 0.2f)});
                    Main.reforgeCooldown = 30;

                    SpawnSparks();
                }

                return true;

                void SpawnSparks()
                {
                    const float max_range = 0.8f;

                    var dark = new Color(245, 174, 70, 100);
                    for (var i = 0; i < 50; i++)
                    {
                        var range = Rand.Next(0f, max_range);

                        var dir = Rand.NextDirection();

                        var velocity = new Vector2(0, ySpeed * dir).RotatedByRandom(range);

                        velocity *= Rand.Next(0.2f, 1.1f);

                        var offset = (size * 0.2f) * Rand.Next(-1f, 1f);
                        offset.X = 0f;

                        ElkParticles.Sparks += new ElkParticles.Spark(
                            position + offset,
                            velocity,
                            Rand.Next(0.8f, 2f),
                            dark,
                            Rand.Next((byte)3)
                        );
                    }

                    var bright = new Color(179, 133, 255, 120);
                    for (var i = 0; i < 7; i++)
                    {
                        var velocity = Rand.NextUnitVector(Rand.Next(1f, 5f));

                        var offset = Vector2.Normalize(velocity) * 17f;

                        ElkParticles.Sparks += new ElkParticles.Spark(
                            position + offset,
                            velocity,
                            Main.rand.NextFloat(2f, 4f),
                            bright,
                            Rand.Next((byte)3)
                        );
                    }

                    ParticleOrchestrator.RequestParticleSpawn(
                        clientOnly: true,
                        ParticleOrchestraType.BestReforge,
                        new ParticleOrchestraSettings
                        {
                            PositionInWorld = position,
                        },
                        Main.myPlayer
                    );
                }

                void SpawnBestSparks()
                {
                    for (var i = 0; i < 2; i++)
                    {
                        ParticleOrchestrator.RequestParticleSpawn(
                            clientOnly: true,
                            ParticleOrchestraType.BestReforge,
                            new ParticleOrchestraSettings
                            {
                                PositionInWorld = position + Rand.NextUnitVector(16f),
                            },
                            Main.myPlayer
                        );

                        ParticleOrchestrator.RequestParticleSpawn(
                            clientOnly: true,
                            ParticleOrchestraType.RainbowRodHit,
                            new ParticleOrchestraSettings
                            {
                                PositionInWorld = position,
                                MovementVector = new Vector2(0f, 70f).RotatedByRandom(0.3f),
                            },
                            Main.myPlayer
                        );
                    }

                    for (var i = 0; i < 25; i++)
                    {
                        var velocity = Rand.NextUnitVector(Rand.Next(2f, 7f));

                        var offset = velocity.Normalized * 13f;

                        var color = Color.FromHsl(0.65f + Rand.Next(0.2f), 1f, 0.65f);

                        ElkParticles.Sparks += new ElkParticles.Spark(
                            position + offset,
                            velocity,
                            Main.rand.NextFloat(3f, 4.5f),
                            color,
                            Rand.Next((byte)3)
                        );
                    }
                }
            }
        );

        c.EmitBrfalse(jumpRetTarget);

        c.EmitRet();

        c.MarkLabel(jumpRetTarget);
    }

    private static void EmitFancyFlashDust_UsesElkName(ILContext il)
    {
        var c = new ILCursor(il);

        var isElkPhrasePopup = c.AddVariable<bool>();

        var widthMultiplier = c.AddVariable<float>();
        var heightMultiplier = c.AddVariable<float>();

        var popupIndex = -1;      // arg
        var textHitboxIndex = -1; // loc

        var jumpXVelocitySettingTarget = c.DefineLabel();
        var jumpYVelocitySettingTarget = c.DefineLabel();

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdarg(out popupIndex),
            i => i.MatchCallvirt<PopupText>(nameof(PopupText.GetTextHitbox)),
            i => i.MatchStloc(out textHitboxIndex)
        );

        c.EmitLdarg(popupIndex);
        c.EmitDelegate(
            static (PopupText popupText) =>
            {
                var index = PopupText.popupText.IndexOf(popupText);

                var item = popupTextItems[index];

                return item is not null && usesElkName[item.type] is not null;
            }
        );
        c.EmitStloc(isElkPhrasePopup);

        SwapPositionsAndInitialVelocities();
        SwapPositionsAndInitialVelocities();

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdfld<Vector2>(nameof(Vector2.X)),
            i => i.MatchStfld<Vector2>(nameof(Vector2.X))
        );

        c.EmitLdloc(isElkPhrasePopup);
        c.EmitBrtrue(jumpXVelocitySettingTarget);

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdfld<Vector2>(nameof(Vector2.X)),
            i => i.MatchStfld<Vector2>(nameof(Vector2.X))
        );

        c.EmitBr(jumpYVelocitySettingTarget);

        c.MarkLabel(jumpXVelocitySettingTarget);

        var vector2YInfo = typeof(Vector2).GetField(nameof(Vector2.Y), BindingFlags.Instance | BindingFlags.Public)!;

        c.EmitLdfld(vector2YInfo);
        c.EmitStfld(vector2YInfo);

        c.MarkLabel(jumpYVelocitySettingTarget);

        return;

        void SwapPositionsAndInitialVelocities()
        {
            c.GotoNext(
                MoveType.After,
                i => i.MatchLdloc(textHitboxIndex),
                i => i.MatchLdfld<Vector2>(nameof(Vector2.X))
            );

            c.GotoNext(
                MoveType.Before,
                i => i.MatchMul()
            );

            c.EmitStloc(widthMultiplier);
            c.EmitLdloc(widthMultiplier);

            c.GotoNext(
                MoveType.After,
                i => i.MatchConvR4(),
                i => i.MatchMul(),
                i => i.MatchAdd()
            );

            c.EmitStloc(heightMultiplier);
            c.EmitLdloc(heightMultiplier);

            c.GotoNext(
                MoveType.After,
                i => i.MatchNewobj<Vector2>()
            );

            c.EmitLdloc(isElkPhrasePopup);

            c.EmitLdarg(popupIndex);
            c.EmitLdloc(textHitboxIndex);

            c.EmitLdloc(widthMultiplier);
            c.EmitLdloc(heightMultiplier);
            c.EmitDelegate(
                static (Vector2 position, bool vertical, PopupText popupText, Vector2 hitbox, float xMultiplier, float yMultiplier) =>
                {
                    if (!vertical)
                    {
                        return position;
                    }

                    return popupText.position + (hitbox * new Vector2(yMultiplier, xMultiplier));
                }
            );

            c.GotoNext(
                MoveType.After,
                i => i.MatchNewobj<Vector2>(),
                i => i.MatchNewobj<Vector2?>()
            );

            c.EmitLdloc(isElkPhrasePopup);
            c.EmitDelegate(
                static (Vector2 velocity, bool vertical) =>
                {
                    if (!vertical)
                    {
                        return velocity;
                    }

                    return new Vector2(velocity.Y, velocity.X);
                }
            );
        }
    }

    private static void DrawItemTextPopups_UsesElkName(ILContext il)
    {
        var c = new ILCursor(il);

        var popupTextIndex = -1;
        var scaleMultiplierIndex = -1;
        var magicAlphaMultiplierIndex = -1;

        var colorIndex = -1;

        ILLabel? contLoopTarget = null;
        ILLabel? jumpDrawEffectsTarget = null;

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
            i => i.MatchLdloc(out int _),
            i => i.MatchDiv(),
            i => i.MatchStloc(out scaleMultiplierIndex)
        );

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdcR4(1f),
            i => i.MatchLdloc(out int _),
            i => i.MatchCallvirt<string>($"get_{nameof(string.Length)}")
        );

        var c2 = c.Clone();

        c.GotoNext(
            i => i.MatchSwitch(out _)
        );

        c.GotoPrev(
            MoveType.After,
            i => i.MatchDiv(),
            i => i.MatchStloc(out int _)
        );

        c.FindNext(
            out _,
            i => i.MatchBr(out _),
            i => i.MatchLdloc(out colorIndex),

            i => i.MatchLdfld<PopupText>(nameof(PopupText.alpha)),
            i => i.MatchLdloc(out magicAlphaMultiplierIndex)
        );

        // Crafting Effects
        {
            c2.GotoPrev(
                MoveType.Before,
                i => i.MatchLdloc(out int _),
                i => i.MatchCall<Color>($"get_{nameof(Color.Black)}"),
                i => i.MatchLdloc(out int _),
                i => i.MatchCall<Color>(nameof(Color.Lerp)),
                i => i.MatchStloc(colorIndex)
            );

            c2.FindPrev(
                out _,
                i => i.MatchLdfld<PopupText>(nameof(PopupText.effectStyle)),
                i => i.MatchLdcI4(5),
                i => i.MatchBneUn(out jumpDrawEffectsTarget)
            );

            Debug.Assert(jumpDrawEffectsTarget is not null);

            c2.EmitLdloc(popupTextIndex);
            c2.EmitLdloca(colorIndex);
            c2.EmitLdloc(magicAlphaMultiplierIndex);

            c2.EmitDelegate(
                static (PopupText popupText, ref Color origColor, float magic) =>
                {
                    var index = PopupText.popupText.IndexOf(popupText);

                    var item = popupTextItems[index];

                    if (item is null || usesElkName[item.type] is not { } phrase)
                    {
                        return false;
                    }

                    var sb = Main.spriteBatch;

                    var phraseSize = phrase.Measure(1f);

                    var fade = Utils.Remap(popupText.framesSinceSpawn, 0f, 55, 0f, 1f);

                    var texture = TextureAssets.Extra[ExtrasID.NinetyEight].Value;
                    var origin = texture.Size() * 0.5f;

                    var scale = popupText.scale * 1.1f;

                    var position = popupText.position - Main.screenPosition + (phraseSize * elk_name_popup_scale * 0.5f);

                    var value = popupText.color;
                    value.Lightness = 1f - fade;
                    value.A = 0;

                    origColor = Color.Lerp(value, origColor, fade);

                    var colorAlpha = Utils.Remap(fade, 0f, 0.1f, 0f, 1f) * Utils.Remap(fade, 0.1f, 1f, 1f, 0f);
                    var whiteAlpha = Utils.Remap(fade, 0f, 0.2f, 0f, 1f) * Utils.Remap(fade, 0.2f, 0.8f, 1f, 0f);
                    if (colorAlpha <= 0f && whiteAlpha <= 0f)
                    {
                        return true;
                    }

                    var e98Size = new Vector2(1f, phraseSize.Y / texture.Width);
                    e98Size *= scale;

                    var slide = new Vector2(0f, Utils.Remap(magic, 1f, 0f, -30f, 30f));
                    slide *= scale;

                    var color = popupText.color * colorAlpha;

                    var white = Color.White with { A = 0 };
                    white *= 0.5f;
                    white *= whiteAlpha;

                    var colorStep = 60 * (phraseSize.Y / 160f);
                    var whiteStep = 20 * (phraseSize.Y / 160f);

                    for (var i = 0; i < 3; i++)
                    {
                        var colorOffset = new Vector2(0f, -colorStep + (colorStep * i));
                        colorOffset += slide;
                        colorOffset *= scale;

                        var whiteOffset = new Vector2(0f, whiteStep + (whiteStep * i));
                        whiteOffset += slide;
                        whiteOffset *= scale;

                        sb.Draw(texture, position + colorOffset, null, color, 0f, origin, e98Size, SpriteEffects.None, 0f);
                        sb.Draw(texture, position + whiteOffset, null, white, 0f, origin, e98Size * 0.5f, SpriteEffects.None, 0f);
                    }

                    return true;
                }
            );

            c2.EmitBrtrue(jumpDrawEffectsTarget);
        }

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

                var fade = (float)Utils.EaseOutCirc(Utils.Remap(popupText.framesSinceSpawn, 0f, 40, 0f, 1f));

                var gradient = popupText.color * scaleMultiplier * popupText.alpha * magic;
                var white = Color.White * multiplier;
                white.A = byte.MaxValue;

                white *= popupText.alpha * magic;

                gradient = Color.Lerp(gradient, Color.White, 1f - fade);

                white = Color.Lerp(white, Color.White, 1f - fade);

                var outline = origColor;

                {
                    var outlineAlpha = popupText.color.A * scaleMultiplier * popupText.alpha;

                    outline.A = (byte)MathF.Lerp(60f, 127f, Utils.GetLerpValue(0f, 255f, outlineAlpha, clamped: true));

                    outline = Color.Lerp(outline, new Color(0, 0, 0, (int)outlineAlpha), 0.25f);
                }

                // The gradient looks particularly ugly with the colors given in this case.
                if (popupText.context == PopupTextContext.ItemReforge_Best)
                {
                    white = gradient;
                }

                var scale = elk_name_popup_scale * popupText.scale;

                var position = popupText.position - Main.screenPosition + (size * elk_name_popup_scale * 0.5f);

                // TODO: Maybe account for rotation? PopupText doesn't use it however so it should be fine.
                sb.DrawItemNamePhrase(phrase, item, position, gradient, white, outline, scale, prefixScale: 1.1f, origin: size * 0.5f);

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

    private static void Update_UsesElkName(ILContext il)
    {
        var c = new ILCursor(il);

        var jumpVelocityUpdatesTarget = c.DefineLabel();

        var selfIndex = -1; // arg
        var collidingFlagIndex = -1; // loc

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdloc(out collidingFlagIndex),
            i => i.MatchBrtrue(out _)
        );

        c.FindNext(
            out _,
            i => i.MatchLdarg(out selfIndex)
        );

        c.MoveAfterLabels();

        c.EmitLdarg(selfIndex);

        c.EmitLdloc(collidingFlagIndex);

        c.EmitDelegate(
            static (PopupText self, bool colliding) =>
            {
                const float epsilon = 0.0001f;

                var index = PopupText.popupText.IndexOf(self);

                var item = popupTextItems[index];

                if (item is null || usesElkName[item.type] is null)
                {
                    return false;
                }

                var sign = MathF.Sign(self.velocity.X);

                if (colliding)
                {
                    if (MathF.Abs(self.velocity.X) < epsilon)
                    {
                        self.velocity.X = Main.rand.NextBool().ToDirectionInt();
                    }

                    sign = MathF.Sign(self.velocity.X);

                    self.velocity.X += 0.5f * sign;
                    self.velocity.Y -= 0.3f;
                }
                else
                {
                    self.velocity *= 0.84f;

                    // Have the popup "remember" what direction it previously moved in.
                    if (MathF.Abs(self.velocity.X) < epsilon)
                    {
                        self.velocity.X = epsilon * sign;
                    }
                }

                return true;
            }
        );

        c.EmitBrtrue(jumpVelocityUpdatesTarget);

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdarg(selfIndex),
            i => i.MatchLdarg(selfIndex)
        );

        c.MarkLabel(jumpVelocityUpdatesTarget);
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

        if (usesElkName[newItem.type] is not { } phrase)
        {
            return index;
        }

        PopupText.popupText[index].velocity.Y -= phrase.Measure(elk_name_popup_scale).Y * 0.1f;

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
            i => i.MatchLdarg(out int _)
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
            i => i.MatchLdloca(out int _),
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
            i => i.MatchLdarg(out int _),
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

    private static void DrawItemNamePhrase(
        this SpriteBatch sb,
        ElkPhrase phrase,
        Item item,
        Vector2 position,
        bool showPrefix = true
    )
    {
        var multiplier = (float)Main.mouseTextColor / byte.MaxValue;

        var gradient = GetRarityColor();

        var white = Color.White * multiplier;

        white.A = byte.MaxValue;

        sb.DrawItemNamePhrase(phrase, item, position, gradient, white, Color.Black, elk_name_tooltip_scale, showPrefix: showPrefix);

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

    private static void DrawItemNamePhrase(
        this SpriteBatch sb,
        ElkPhrase phrase,
        Item item,
        Vector2 position,
        Color gradient,
        Color white,
        Color outline,
        float scale,
        float prefixScale = 0.9f,
        Vector2 origin = default,
        bool showPrefix = true
    )
    {
        var rarityShader = Assets.Elk.Language.RarityGradient.CreateRarityGradientShader();

        var size = phrase.Measure(scale);

        sb.DrawPhraseOutline(phrase, position, outline, scale, origin, spread: 1.5f, directions: 8);

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
                outline,
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

            var prefixPosition = new Vector2(position.X + (10f * scale), position.Y + size.Y - (lastCharacterHeight * 0.5f * scale));
            prefixPosition -= origin * scale;

            var prefixRotation = -MathF.PiOver2;

            var prefixSize = font.MeasureString(prefixText);
            prefixScale *= scale;

            var prefixOrigin = prefixSize * new Vector2(0.5f, 1f);

            ChatManager.DrawColorCodedStringWithShadow(
                sb,
                font,
                prefixText,
                prefixPosition,
                white,
                outline,
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

    private record struct ShimmerSpike(Point Position, float Height, float LifeTime, float LifeTimeIncrement) : IUpdatingParticle
    {
        public bool Update()
        {
            LifeTime += LifeTimeIncrement;

            return LifeTime <= 1f;
        }
    }

    private static UpdatingParticleHandler<ShimmerSpike> spikes = new(128);

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

                    var dest = new Rectangle((int)position.X, (int)((position.Y - height)), 16, (int)height);

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

                var position = item.Bottom.ToTileCoordinates();

                // Can be fairly reasonably assumed that the bottom of the item is the top tile of the shimmer
                spikes += new ShimmerSpike(position, 64f, 0f, 0.06f);

                spikes += new ShimmerSpike(new Point(position.X - 1, position.Y), 32f, 0f, 0.04f);
                spikes += new ShimmerSpike(new Point(position.X + 1, position.Y), 32f, 0f, 0.04f);
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

        var velocity = -self.velocity;
        velocity.Y = -9f;

        self.velocity = velocity;

        self.shimmerTime = 0;
        self.shimmered = false;
    }
}
