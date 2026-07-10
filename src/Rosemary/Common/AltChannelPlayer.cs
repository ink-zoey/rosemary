using System.IO;
using Daybreak.Networking;
using Terraria;
using Terraria.Graphics.Capture;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Common;

file sealed class AltChannelPlayer : ModPlayer
{
    private record struct Packet(int WhoAmI) : IPacket<Packet>
    {
        public void Write(BinaryWriter writer)
        {
            writer.Write(WhoAmI);
            writer.Write(Main.player[WhoAmI].AltChannel);
        }

        public static void Receive(BinaryReader reader, int sender)
        {
            var whoAmI = reader.ReadInt32();

            if (Main.netMode == NetmodeID.Server)
            {
                whoAmI = sender;
            }

            var player = Main.player[whoAmI];

            player.GetModPlayer<AltChannelPlayer>().AltChannel = reader.ReadBoolean();
        }
    }

    public bool AltChannel { get; private set; }

    public override void Load()
    {
        On_Player.ItemCheck_ManageRightClickFeatures += ItemCheck_ManageRightClickFeatures_AltChannel;
    }

    private static void ItemCheck_ManageRightClickFeatures_AltChannel(On_Player.orig_ItemCheck_ManageRightClickFeatures orig, Player self)
    {
        // Vanilla condition taken from the method, baring left click checks to allow using both buttons at the same time.
        var clicking = self.selectedItem != ItemID.Heart
                    && self.controlUseTile
                    && self.whoAmI == Main.myPlayer
                    && !self.tileInteractionHappened
                    && !self.mouseInterface
                    && !CaptureManager.Instance.Active
                    && (!Main.mouseRightRelease || !Main.HoveringAnInteractable)
                    && !Main.LocalPlayerHasPendingInventoryActions();

        self.GetModPlayer<AltChannelPlayer>().AltChannel = clicking;

        orig(self);
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        new Packet(Player.whoAmI).Send(PacketDestination.From(toWho, fromWho));
    }

    public override void CopyClientState(ModPlayer targetCopy)
    {
        var clone = (AltChannelPlayer)targetCopy;

        clone.AltChannel = AltChannel;
    }

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        var clone = (AltChannelPlayer)clientPlayer;

        if (AltChannel != clone.AltChannel)
        {
            SyncPlayer(-1, Main.myPlayer, false);
        }
    }
}

public static class AltChannelPlayerExtensions
{
    extension(Player player)
    {
        /// <summary>
        ///      <see langword="true"/> if the player is using their alt fire key; can be used in conjunction with <see cref="Player.channel"/>.
        /// </summary>
        public bool AltChannel => player.GetModPlayer<AltChannelPlayer>().AltChannel;
    }
}
