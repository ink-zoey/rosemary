using Daybreak.Hooks;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Vanity.Content;

public sealed class SiffrinTransform : ModItem
{
    public override string Texture => Assets.Vanity.Hat.KEY;

    public override string LocalizationCategory => "Content";

    public override void Load()
    {
        if (Main.dedServ)
        {
            return;
        }

        EquipLoader.AddEquipTexture(Mod, Assets.Vanity.Undershirt_Equip.KEY, EquipType.Body, this);
        EquipLoader.AddEquipTexture(Mod, Assets.Vanity.Leggings_Equip.KEY, EquipType.Legs, this);

        On_PlayerDrawSet.BoringSetup_2 += BoringSetup_2_SkinColor;
        On_PlayerDrawLayers.DrawPlayer_21_Head += DrawPlayer_21_Head_HairStyle;
    }

    private static void DrawPlayer_21_Head_HairStyle(On_PlayerDrawLayers.orig_DrawPlayer_21_Head orig, ref PlayerDrawSet drawInfo)
    {
        if (!IsVisible(drawInfo))
        {
            orig(ref drawInfo);
            return;
        }

        var prior = drawInfo.drawPlayer.hair;
        drawInfo.drawPlayer.hair = ModContent.GetInstance<SiffrinHairstyle>().Type;
        {
            orig(ref drawInfo);
        }
        drawInfo.drawPlayer.hair = prior;
    }

    private static void BoringSetup_2_SkinColor(
        On_PlayerDrawSet.orig_BoringSetup_2 orig,
        ref PlayerDrawSet self,
        Player player,
        List<DrawData> drawData,
        List<int> dust,
        List<int> gore,
        Vector2 drawPosition,
        float shadowOpacity,
        float rotation,
        Vector2 rotationOrigin
    )
    {
        if (!IsVisible(self))
        {
            orig(ref self, player, drawData, dust, gore, drawPosition, shadowOpacity, rotation, rotationOrigin);
            return;
        }

        var priorEye = player.eyeColor;
        var priorSkin = player.skinColor;
        var priorHair = player.hairColor;
        player.eyeColor = Color.Black;
        player.skinColor = new Color(210, 210, 210, byte.MaxValue);
        player.hairColor = Color.White;
        {
            orig(ref self, player, drawData, dust, gore, drawPosition, shadowOpacity, rotation, rotationOrigin);
        }
        player.hairColor = priorHair;
        player.skinColor = priorSkin;
        player.eyeColor = priorEye;

        self.hairDyePacked = 0;
    }

    private static bool IsVisible(PlayerDrawSet drawInfo) => IsVisible(drawInfo.drawPlayer);

    private static bool IsVisible(Player player)
    {
        return player.body == EquipLoader.GetEquipSlot(ModContent.GetInstance<ModImpl>(), ModContent.GetInstance<SiffrinTransform>().Name, EquipType.Body);
    }

    public override void SetStaticDefaults()
    {
        // var equipSlotBody = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
        var equipSlotLegs = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);

        ArmorIDs.Legs.Sets.HidesBottomSkin[equipSlotLegs] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 20;

        Item.accessory = true;
        Item.vanity = true;
    }

    public override void UpdateVisibleAccessory(Player player, bool hideVisual)
    {
        if (hideVisual)
        {
            return;
        }
        
        player.body = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
        player.legs = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
    }

    private sealed class CloakDrawLayer : PlayerDrawLayer
    {
        [OnLoad]
        private new static void Load()
        {
            IL_Player.PlayerFrame += PlayerFrame_ForceBodyFrame_SiffrinHover;
            On_PlayerDrawLayers.DrawPlayer_28_ArmOverItem += DrawPlayer_28_ArmOverItem_ArmVisuals;
            On_PlayerDrawLayers.DrawPlayer_12_SkinComposite_BackArmShirt += DrawPlayer_12_SkinComposite_BackArmShirt_HideArms;
        }

        private static void PlayerFrame_ForceBodyFrame_SiffrinHover(ILContext il)
        {
            var c = new ILCursor(il);

            var playerIndex = -1; // arg
            ILLabel? jumpJumpFramingTarget = null;

            c.GotoNext(
                MoveType.After,
                i => i.MatchLdarg(out playerIndex),
                i => i.MatchLdfld<Player>(nameof(Player.wings)),
                i => i.MatchLdcI4(22),
                i => i.MatchBeq(out _)
            );

            c.GotoNext(
                MoveType.Before,
                i => i.MatchLdarg(playerIndex),
                i => i.MatchLdfld<Player>(nameof(Player.wings)),
                i => i.MatchLdcI4(22),
                i => i.MatchBeq(out _)
            );

            c.GotoPrev(
                MoveType.Before,
                i => i.MatchLdarg(playerIndex),
                i => i.MatchLdfld<Player>(nameof(Player.sliding))
            );

            c.FindNext(
                out _,
                i => i.MatchBr(out jumpJumpFramingTarget)
            );
            Debug.Assert(jumpJumpFramingTarget is not null);

            c.MoveAfterLabels();

            c.EmitLdarg(playerIndex);
            c.EmitDelegate(
                static (Player player) =>
                {
                    if (!player.mount.Active
                     || player.mount.Type != ModContent.MountType<SiffrinHoverMount>()
                     || !IsVisible(player))
                    {
                        return false;
                    }

                    player.bodyFrame.Y = 0;
                    return true;
                }
            );
            c.EmitBrtrue(jumpJumpFramingTarget);
        }

        private static void DrawPlayer_12_SkinComposite_BackArmShirt_HideArms(On_PlayerDrawLayers.orig_DrawPlayer_12_SkinComposite_BackArmShirt orig, ref PlayerDrawSet drawInfo)
        {
            if (IsVisible(drawInfo) && !ShowsArm(drawInfo))
            {
                return;
            }

            orig(ref drawInfo);
        }

        private static void DrawPlayer_28_ArmOverItem_ArmVisuals(On_PlayerDrawLayers.orig_DrawPlayer_28_ArmOverItem orig, ref PlayerDrawSet drawInfo)
        {
            if (!IsVisible(drawInfo))
            {
                orig(ref drawInfo);
                return;
            }

            if (!ShowsArm(drawInfo))
            {
                return;
            }

            const float offset = -4;

            var dir = drawInfo.playerEffect.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f;

            var pos = new Vector2(0, offset * dir);

            var prior = drawInfo.bodyVect;
            drawInfo.bodyVect += pos;
            {
                orig(ref drawInfo);
            }
            drawInfo.bodyVect = prior;
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => IsVisible(drawInfo);

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FaceAcc);

        // There may be a friendlier way to go about this.
        private static readonly bool[,] visible_arms_by_frame = new[,]
        {
            {false, false, false, true,  true,  true,  true,  true,  false },
            {false, false, true,  false, false, false, false, true,  false },
            {false, false, false, false, false, false, false, true,  false },
            {false, false, false, false, false, false, false, true,  false },
        };

        private static bool ShowsArm(PlayerDrawSet drawInfo)
        {
            var armFrame = drawInfo.compFrontArmFrame;
            var (frameX, frameY) = new Point(armFrame.X / 40, armFrame.Y / 56);

            // Index it in reverse because 2D arrays hate me.
            return visible_arms_by_frame[frameY, frameX];
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            var player = drawInfo.drawPlayer;

            var dir = player.Directions;

            var bodyOffset = Main.OffsetsPlayerHeadgear[drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height].Y;

            var bodyPosition = new Vector2(
                                   (int)(drawInfo.Position.X - Main.screenPosition.X - (player.bodyFrame.Width * 0.5f) + (player.width * 0.5f)),
                                   (int)(drawInfo.Position.Y - Main.screenPosition.Y + player.height - player.bodyFrame.Height + 4f)
                               )
                             + drawInfo.drawPlayer.bodyPosition
                             + (drawInfo.drawPlayer.bodyFrame.Size() * 0.5f);

            var position = bodyPosition + new Vector2(8, (bodyOffset + 24) * dir.Y);

            if ((int)drawInfo.drawPlayer.gravDir == -1)
            {
                position.Y += player.height - player.bodyPosition.Y - 2;
            }

            var texture = Assets.Vanity.Cloak_Equip.Asset.Value;

            var cloakFrame = new Rectangle(ShowsArm(drawInfo) ? 26 : 0, 0, 24, 22);

            var cloakData = new DrawData(
                texture,
                position,
                cloakFrame,
                drawInfo.colorArmorBody,
                drawInfo.drawPlayer.bodyRotation,
                drawInfo.bodyVect,
                1f,
                drawInfo.playerEffect
            )
            {
                shader = drawInfo.cBody,
            };
            drawInfo.DrawDataCache.Add(cloakData);

            var collarFrame = new Rectangle(52, 0, 22, 22);

            var collarData = new DrawData(
                texture,
                position,
                collarFrame,
                drawInfo.colorArmorBody,
                drawInfo.drawPlayer.bodyRotation,
                drawInfo.bodyVect,
                1f,
                drawInfo.playerEffect
            )
            {
                shader = drawInfo.cBody,
            };
            drawInfo.DrawDataCache.Add(collarData);
        }
    }
}
