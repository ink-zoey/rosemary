using Daybreak.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        On_PlayerDrawSet.HeadOnlySetup += HeadOnlySetup_SkinColor;
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

    private static void HeadOnlySetup_SkinColor(
        On_PlayerDrawSet.orig_HeadOnlySetup orig,
        ref PlayerDrawSet self,
        Player player,
        List<DrawData> drawData,
        List<int> dust,
        List<int> gore,
        float x,
        float y,
        float alpha,
        float scale
    )
    {
        if (!IsVisible(player))
        {
            orig(ref self, player, drawData, dust, gore, x, y, alpha, scale);
            return;
        }

        var priorEye = player.eyeColor;
        var priorSkin = player.skinColor;
        var priorHair = player.hairColor;
        player.eyeColor = Color.Black;
        player.skinColor = new Color(210, 210, 210, byte.MaxValue);
        player.hairColor = Color.White;
        {
            orig(ref self, player, drawData, dust, gore, x, y, alpha, scale);
        }
        player.hairColor = priorHair;
        player.skinColor = priorSkin;
        player.eyeColor = priorEye;

        self.hairDyePacked = 0;
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
        if (!IsVisible(player))
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

    private sealed class CloakRotationPlayer : ModPlayer
    {
        public float Rotation;
        private float rotationVelocity;

        public override void PostUpdate()
        {
            if (!Player.mount.Active
             || Player.mount.Type != ModContent.MountType<SiffrinHoverMount>()
             || !IsVisible(Player)
            )
            {
                return;
            }

            const float max_rotation = MathF.PI * 0.132f;
            const float spring_strength = 0.01f;
            const float min_dampening = 0.98f;
            const float max_dampening = 0.92f;
            const float wind_freq = 0.06f;

            rotationVelocity += MathF.Pow(MathF.Abs(Player.velocity.X) / 16f, 6) * 0.07f * MathF.Sign(Player.velocity.X);

            var windInterpolator = MathF.Sin((float)Main.timeForVisualEffects * wind_freq);
            windInterpolator += 1f;
            windInterpolator *= 0.5f;

            rotationVelocity += -Main.WindForVisuals * MathF.Lerp(0.002f, 0.0045f, windInterpolator);

            var displacement = -Rotation;

            var dist = Math.Abs(Rotation);
            var t = MathF.Saturate(dist / max_rotation);
            var dampening = MathF.Lerp(min_dampening, max_dampening, MathF.Pow(t, 2));

            rotationVelocity += displacement * spring_strength;
            rotationVelocity *= dampening;

            Rotation += rotationVelocity;

            Rotation = MathF.Clamp(Rotation, -max_rotation, max_rotation);
            rotationVelocity = MathF.Clamp(rotationVelocity, -0.05f, 0.05f);
        }
    }

    private sealed class CloakDrawLayer : PlayerDrawLayer
    {
        [OnLoad(Side = ModSide.Client)]
        private new static void Load()
        {
            IL_Player.PlayerFrame += PlayerFrame_ForceBodyFrame_SiffrinHover;
            On_PlayerDrawLayers.DrawPlayer_28_ArmOverItem += DrawPlayer_28_ArmOverItem_ArmVisuals;
            IL_PlayerDrawLayers.DrawPlayer_28_ArmOverItemComposite += DrawPlayer_28_ArmOverItem_OffsetArmHack;
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

        private static void DrawPlayer_28_ArmOverItem_OffsetArmHack(ILContext il)
        {
            var c = new ILCursor(il);

            var baseVectorIndex = -1; // loc
            var drawInfoIndex = -1;   // arg

            c.GotoNext(
                MoveType.After,
                i => i.MatchCall(typeof(PlayerDrawLayers), nameof(PlayerDrawLayers.GetCompositeOffset_FrontArm)),
                i => i.MatchStloc(out _)
            );

            c.GotoNext(
                MoveType.Before,
                i => i.MatchLdloc(out baseVectorIndex),
                i => i.MatchLdarg(out drawInfoIndex)
            );

            c.EmitLdloca(baseVectorIndex);
            c.EmitLdarg(drawInfoIndex);
            c.EmitDelegate(
                static (ref Vector2 vector, ref PlayerDrawSet drawInfo) =>
                {
                    if (IsVisible(drawInfo) && !ShowsArm(drawInfo))
                    {
                        return;
                    }

                    var armFrame = drawInfo.compFrontArmFrame;
                    var (frameX, _) = new Point(armFrame.X / 40, armFrame.Y / 56);

                    if (frameX == 7)
                    {
                        vector.Y += 6 * drawInfo.drawPlayer.gravDir;
                    }
                }
            );
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

            var armFrame = drawInfo.compFrontArmFrame;
            var (frameX, _) = new Point(armFrame.X / 40, armFrame.Y / 56);

            var dir = drawInfo.playerEffect.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f;

            var pos = new Vector2(0, offset * dir);

            if (frameX == 7)
            {
                pos = Vector2.Zero;
            }

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
                             + (drawInfo.drawPlayer.bodyFrame.Size() * 0.5f).Floor();

            var position = bodyPosition + new Vector2(0, (bodyOffset + 7) * dir.Y);

            var texture = Assets.Vanity.Cloak_Equip.Asset.Value;

            var showsArms = ShowsArm(drawInfo);

            if (!player.mount.Active
             || player.mount.Type != ModContent.MountType<SiffrinHoverMount>()
             || (int)player.gravDir == -1
            )
            {
                var cloakFrame = new Rectangle(showsArms ? 26 : 0, 0, 24, 22);

                var cloakData = new DrawData(
                    texture,
                    position,
                    cloakFrame,
                    drawInfo.colorArmorBody,
                    0f,
                    cloakFrame.Size() * 0.5f,
                    1f,
                    drawInfo.playerEffect
                )
                {
                    shader = drawInfo.cBody,
                };
                drawInfo.DrawDataCache.Add(cloakData);
            }
            else
            {
                DrawRotatingCloak(ref drawInfo);
            }

            var collarFrame = new Rectangle(52, 0, 24, 22);

            var collarData = new DrawData(
                texture,
                position,
                collarFrame,
                drawInfo.colorArmorBody,
                0f,
                collarFrame.Size() * 0.5f,
                1f,
                drawInfo.playerEffect
            )
            {
                shader = drawInfo.cBody,
            };
            drawInfo.DrawDataCache.Add(collarData);

            return;

            void DrawRotatingCloak(ref PlayerDrawSet drawInfo)
            {
                var cloakFrame = new Rectangle(78, 0, 36, 22);

                var cloakOrigin = new Vector2(18, 4);

                var rotation = player.GetModPlayer<CloakRotationPlayer>().Rotation;

                var offset = new Vector2(0, -2);

                var cloakData = new DrawData(
                    texture,
                    position + offset,
                    cloakFrame,
                    drawInfo.colorArmorBody,
                    rotation,
                    cloakOrigin,
                    1f,
                    drawInfo.playerEffect
                )
                {
                    shader = drawInfo.cBody,
                };
                drawInfo.DrawDataCache.Add(cloakData);

                if (showsArms)
                {
                    var overlayFrame = new Rectangle(116, 0, 24, 28);

                    var overlayData = new DrawData(
                        texture,
                        position,
                        overlayFrame,
                        drawInfo.colorArmorBody,
                        0f,
                        new Vector2(12, 11),
                        1f,
                        drawInfo.playerEffect
                    )
                    {
                        shader = drawInfo.cBody,
                    };
                    drawInfo.DrawDataCache.Add(overlayData);
                }
            }
        }
    }

    private sealed class EyePatchDrawLayer : PlayerDrawLayer
    {
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => IsVisible(drawInfo);

        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.FaceAcc);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            var player = drawInfo.drawPlayer;

            if (player.direction != -1)
            {
                return;
            }

            var dir = player.Directions;

            var headOffset = Main.OffsetsPlayerHeadgear[player.bodyFrame.Y / player.bodyFrame.Height].Y;

            var helmetOffset = Vector2.Zero;
            player.ApplyHeadOffsetFromMount(ref helmetOffset);
            helmetOffset += drawInfo.helmetOffset;

            var headPosition = helmetOffset
                             + new Vector2(
                                   (int)(drawInfo.Position.X - Main.screenPosition.X - (player.bodyFrame.Width * 0.5f) + (player.width * 0.5f)),
                                   (int)(drawInfo.Position.Y - Main.screenPosition.Y + player.height - (player.bodyFrame.Height + 4f)))
                             + drawInfo.drawPlayer.headPosition
                             + drawInfo.headVect.Floor();

            var position = headPosition + new Vector2(-2, (headOffset + 6) * (drawInfo.headOnlyRender ? 1f : dir.Y));

            if ((int)player.gravDir == -1 && !drawInfo.headOnlyRender)
            {
                position.Y += player.height - player.headPosition.Y - 8;
            }

            var texture = Assets.Vanity.EyePatch_Equip.Asset.Value;


            var hatData = new DrawData(
                texture,
                position,
                null,
                drawInfo.colorArmorHead,
                player.headRotation,
                texture.Size() * 0.5f,
                1f,
                drawInfo.playerEffect
            )
            {
                shader = drawInfo.cHead,
            };
            drawInfo.DrawDataCache.Add(hatData);
        }
    }
}
