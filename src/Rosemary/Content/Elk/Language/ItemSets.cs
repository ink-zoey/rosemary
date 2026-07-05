using Daybreak.Common.CIL;
using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Rosemary.Common;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace Rosemary.Content.Elk;

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
        IL_PopupText.Update += Update_UsesElkName;

        IL_PopupText.NewText_PopupTextContext_Item_Vector2_int_bool_bool += NewText_UsesElkName;

        IL_PopupText.DrawItemTextPopups += DrawItemTextPopups_UsesElkName;

        IL_PopupText.EmitFancyFlashDust += EmitFancyFlashDust_UsesElkName;

        IL_Main.ReforgeItemInReforgeSlot += ReforgeItemInReforgeSlot_UsesElkName;
    }

#region UsesElkName
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
                    SoundEngine.PlaySound(in SoundID.Item37);
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

                        ElkForegroundParticles.Sparks += new ElkForegroundParticles.Spark(
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

                        ElkForegroundParticles.Sparks += new ElkForegroundParticles.Spark(
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

                        var offset = Vector2.Normalize(velocity) * 13f;

                        var color = Main.hslToRgb(0.65f + (Rand.Next(0.2f)), 1f, 0.65f);

                        ElkForegroundParticles.Sparks += new ElkForegroundParticles.Spark(
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

                var outline = origColor;

                {
                    var outlineAlpha = popupText.color.A * scaleMultiplier * popupText.alpha;

                    outline.A = (byte)MathHelper.Lerp(60f, 127f, Utils.GetLerpValue(0f, 255f, outlineAlpha, clamped: true));

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

        if (usesElkName[newItem.type] is null)
        {
            return index;
        }

        PopupText.popupText[index].velocity.Y -= 20f;

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

            var prefixRotation = -MathHelper.PiOver2;

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
}
