using Daybreak.Hooks;
using Daybreak.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rosemary.Common;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

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
        EquipLoader.AddEquipTexture(Mod, Assets.Blank.KEY, EquipType.Head, this);

        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += Draw_HatStyleToggle;
        On_ItemSlot.Handle_ItemArray_int_int_bool += Handle_BlockInput;
    }

    private static Vector2 GetHatStyleToggleOffset()
    {
        var slotSize = TextureAssets.InventoryBack.Size() * Main.inventoryScale;

        var offset = -Assets.Vanity.Hat_StyleToggle.Asset.Value.Size() * 0.75f;

        return slotSize + offset;
    }

    private void Handle_BlockInput(On_ItemSlot.orig_Handle_ItemArray_int_int_bool orig, Item[] inv, int context, int slot, bool allowInteract)
    {
        if (context != 8 && context != 9 || inv[slot].type != ModContent.ItemType<SiffrinHat>())
        {
            orig(inv, context, slot, allowInteract);
            return;
        }

        const float slot_size = 56f;

        var texture = Assets.Vanity.Hat_StyleToggle.Asset.Value;

        var index = (slot % 10);

        var inventoryTop = 174 + Main.mH;

        var position = new Vector2(Main.screenWidth - 64 - 28, inventoryTop + ((slot_size * index) * Main.inventoryScale)).Floor();

        if (context == 9)
        {
            position.X -= 47;
        }

        var buttonPosition = position;
        buttonPosition += GetHatStyleToggleOffset();
        buttonPosition = buttonPosition.Floor();

        var bounds = new Rectangle((int)buttonPosition.X, (int)buttonPosition.Y, texture.Width, texture.Height);

        if (bounds.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
        {
            return;
        }

        orig(inv, context, slot, allowInteract);
    }

    private static void Draw_HatStyleToggle(
        On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig,
        SpriteBatch spriteBatch,
        Item[] inv,
        int context,
        int slot,
        Vector2 position,
        Color lightColor
    )
    {
        orig(spriteBatch, inv, context, slot, position, lightColor);

        if (context != 8 && context != 9 || inv[slot].type != ModContent.ItemType<SiffrinHat>())
        {
            return;
        }

        var sb = Main.spriteBatch;

        var texture = Assets.Vanity.Hat_StyleToggle.Asset.Value;

        var buttonPosition = position;
        buttonPosition += GetHatStyleToggleOffset();
        buttonPosition = buttonPosition.Floor();

        var bounds = new Rectangle((int)buttonPosition.X, (int)buttonPosition.Y, texture.Width, texture.Height);

        var player = AccessorySlotLoader.Player;

        var modPlayer = player.GetModPlayer<HatStylePlayer>();

        if (bounds.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface)
        {
            player.mouseInterface = true;

            if ((Main.mouseLeft && Main.mouseLeftRelease) || (Main.mouseRight && Main.mouseRightRelease))
            {
                modPlayer.Style += Main.mouseRight ? -1 : 1;
                modPlayer.Style %= HatStylePlayer.MAX_STYLE;
                if (modPlayer.Style < 0)
                {
                    modPlayer.Style = HatStylePlayer.MAX_STYLE - 1;
                }

                SoundEngine.PlaySound(in SoundID.MenuTick);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    modPlayer.SyncPlayer(-1, player.whoAmI, false);
                }
            }

            Main.HoverItem = new Item();
            Main.hoverItemName = Mods.RosemaryVanity.Content.SiffrinHat.Name.GetChildTextValue(modPlayer.Style.ToString());
        }

        sb.Draw(texture, buttonPosition, Color.White * 0.7f);
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
            var texture = Assets.Vanity.Hat_Equip.Asset.Value;

            var player = drawInfo.drawPlayer;

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

            var position = headPosition + new Vector2(0, headOffset * (drawInfo.headOnlyRender ? 1f : dir.Y));

            if ((int)player.gravDir == -1 && !drawInfo.headOnlyRender)
            {
                position.Y += player.height - player.headPosition.Y + 8;
            }

            var style = player.GetModPlayer<HatStylePlayer>().Style;

            var frame = texture.Frame(2, HatStylePlayer.MAX_STYLE, FrameX, style);
            frame.Width -= 2;

            var origin = new Vector2(40, 26);

            var hatData = new DrawData(
                texture,
                position,
                frame,
                drawInfo.colorArmorHead,
                0f, // Unlikely to play nicely with rotation anyway.
                origin,
                1f,
                drawInfo.playerEffect
            )
            {
                shader = drawInfo.cHead,
            };
            drawInfo.DrawDataCache.Add(hatData);

            var modPlayer = drawInfo.drawPlayer.GetModPlayer<HatStylePlayer>();

            drawInfo.hatHair = modPlayer.Style != HatStylePlayer.MAX_STYLE - 1;
            drawInfo.fullHair = !drawInfo.hatHair;
        }
    }

    private sealed class HatFrontDrawLayer : HatBehindDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FaceAcc);

        protected override int FrameX => 1;
    }
}

file sealed class HatStylePlayer : ModPlayer
{
    private struct Packet : IPacket<Packet>
    {
        public int WhoAmI;

        public Packet() : this(-1)
        { }

        public Packet(int whoAmI)
        {
            WhoAmI = whoAmI;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(WhoAmI);
            writer.Write(Main.player[WhoAmI].GetModPlayer<HatStylePlayer>().Style);
        }

        public static void Receive(BinaryReader reader, int sender)
        {
            var whoAmI = reader.ReadInt32();

            if (Main.netMode == NetmodeID.Server)
            {
                whoAmI = sender;
            }

            var player = Main.player[whoAmI];

            player.GetModPlayer<HatStylePlayer>().Style = reader.ReadInt32();
        }
    }

    public const int MAX_STYLE = 7;

    public int Style;

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        new Packet(Player.whoAmI).Send(PacketDestination.From(toWho, fromWho));
    }

    public override void CopyClientState(ModPlayer targetCopy)
    {
        var clone = (HatStylePlayer)targetCopy;

        clone.Style = Style;
    }

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        var clone = (HatStylePlayer)clientPlayer;

        if (Style != clone.Style)
        {
            SyncPlayer(-1, Main.myPlayer, false);
        }
    }

    public override void SaveData(TagCompound tag)
    {
        tag[nameof(Style)] = Style;
    }

    public override void LoadData(TagCompound tag)
    {
        Style = tag.GetInt(nameof(Style));
    }
}
