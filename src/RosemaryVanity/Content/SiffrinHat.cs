using Daybreak.Hooks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Vanity.Content;

public sealed class SiffrinHat : ModItem
{
    public override string Texture => Assets.Vanity.Hat.KEY;

    public override string LocalizationCategory => "Content";

    public override void Load()
    {
        if (Main.dedServ)
        {
            return;
        }

        // Irrelevant, we'll be overriding this anyway.
        EquipLoader.AddEquipTexture(Mod, Assets.Vanity.Hat.KEY, EquipType.Head, this);
    }

    public override void SetStaticDefaults()
    {
        var equipSlotHead = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);

        ArmorIDs.Head.Sets.DrawHatHair[equipSlotHead] = true;
        ArmorIDs.Head.Sets.DrawsBackHairWithoutHeadgear[equipSlotHead] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 20;

        Item.vanity = true;

        if (Main.dedServ)
        {
            return;
        }

        var equipSlotHead = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);

        Item.headSlot = equipSlotHead;
    }

    private class HatBehindDrawLayer : PlayerDrawLayer
    {
        [OnLoad]
        private new static void Load()
        {
            On_PlayerDrawLayers.DrawPlayer_21_Head += DrawPlayer_21_Head_HideArmor;
        }

        private static void DrawPlayer_21_Head_HideArmor(On_PlayerDrawLayers.orig_DrawPlayer_21_Head orig, ref PlayerDrawSet drawInfo)
        {
            // Vanilla hat rendering should not take place.
            if (!IsVisible(drawInfo))
            {
                orig(ref drawInfo);
                return;
            }

            var prior = drawInfo.drawPlayer.head;
            drawInfo.drawPlayer.head = -1;
            {
                orig(ref drawInfo);
            }
            drawInfo.drawPlayer.head = prior;
        }

        private static bool IsVisible(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.head == EquipLoader.GetEquipSlot(ModContent.GetInstance<ModImpl>(), ModContent.GetInstance<SiffrinHat>().Name, EquipType.Head);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => IsVisible(drawInfo);

        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Head);

        public override bool IsHeadLayer => true;

        protected virtual int FrameX => 0;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            var headOffset = Main.OffsetsPlayerHeadgear[drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height].Y;

            var headPosition = drawInfo.drawPlayer.GetHelmetDrawOffset()
                             + new Vector2(
                                   (int)(drawInfo.Position.X - Main.screenPosition.X - drawInfo.drawPlayer.bodyFrame.Width * 0.5f + drawInfo.drawPlayer.width * 0.5f),
                                   (int)(drawInfo.Position.Y - Main.screenPosition.Y + drawInfo.drawPlayer.height - drawInfo.drawPlayer.bodyFrame.Height + 4f))
                             + drawInfo.drawPlayer.headPosition
                             + drawInfo.headVect
                             + drawInfo.helmetOffset;

            var position = headPosition + new Vector2(4, headOffset - 4);

            var texture = Assets.Vanity.Hat_Equip.Asset.Value;

            var frame = texture.Frame(2, 1, FrameX, 0);

            var hatData = new DrawData(
                texture,
                position,
                frame,
                drawInfo.colorArmorHead,
                drawInfo.drawPlayer.headRotation,
                drawInfo.headVect,
                1f,
                drawInfo.playerEffect
            )
            {
                shader = drawInfo.cHead,
            };
            drawInfo.DrawDataCache.Add(hatData);
        }
    }

    private sealed class HatFrontDrawLayer : HatBehindDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        protected override int FrameX => 1;
    }
}
