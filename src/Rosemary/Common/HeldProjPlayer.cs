using System.Diagnostics;
using Terraria;
using Terraria.ModLoader;

namespace Rosemary.Common;

// Certain logic requires having Player,heldProj without being reset at the very start of Player.Update.
file sealed class HeldProjPlayer : ModPlayer
{
    public int PriorHeldProj { get; private set; }

    public override void Load()
    {
        On_Player.Update += Update_PriorHeldProj;
    }

    // PreUpdate is not early enough.
    [StackTraceHidden]
    private static void Update_PriorHeldProj(On_Player.orig_Update orig, Player self, int i)
    {
        self.GetModPlayer<HeldProjPlayer>().PriorHeldProj = self.heldProj;

        orig(self, i);
    }
}

public static class HeldProjPlayerExtensions
{
    extension(Player player)
    {
        public int PriorHeldProj => player.GetModPlayer<HeldProjPlayer>().PriorHeldProj;
    }
}
