using MonoMod.Cil;
using Rosemary.Core;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Common;

file sealed class AltChannelPlayer : ModPlayer
{
    private record struct Packet(int WhoAmI) : IPacketHandler<Packet>
    {
        public void Write(BinaryWriter writer)
        {
            writer.Write(WhoAmI);
            writer.Write(Main.player[WhoAmI].AltChannel);
        }

        public void Read(BinaryReader reader, int sender)
        {
            WhoAmI = reader.ReadInt32();

            if (Main.netMode == NetmodeID.Server)
            {
                WhoAmI = sender;
            }

            var player = Main.player[WhoAmI];

            player.GetModPlayer<AltChannelPlayer>().AltChannel = reader.ReadBoolean();
        }
    }

    public bool AltChannel { get; private set; }

    public override void Load()
    {
        IL_Player.ItemCheck_ManageRightClickFeatures += ItemCheck_ManageRightClickFeatures_AltChannel;
    }

    private static void ItemCheck_ManageRightClickFeatures_AltChannel(ILContext il)
    {
        var c = new ILCursor(il);

        var playerIndex = -1; // arg
        var flag2Index = -1;  // loc

        c.GotoNext(
            i => i.MatchLdarg(out playerIndex),
            i => i.MatchLdfld<Player>(nameof(Player.altFunctionUse))
        );

        c.GotoPrev(
            MoveType.Before,
            i => i.MatchLdloc(out flag2Index)
        );

        c.MoveAfterLabels();

        c.EmitLdarg(playerIndex);
        c.EmitLdloc(flag2Index);
        c.EmitDelegate(
            static (Player player, bool altChannel) =>
            {
                player.GetModPlayer<AltChannelPlayer>().AltChannel = altChannel;
            }
        );
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        new Packet(Player.whoAmI).Send(toWho, fromWho);
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
        public bool AltChannel => player.GetModPlayer<AltChannelPlayer>().AltChannel;
    }
}
