using Daybreak.Hooks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content;

public static class MiscItemSets
{
    // Should be noted that this works above the encumbering stone and will disable pickups for hearts and mana.
    private static bool[] blocksItemPickupsWhenHeld = [];

    private static Mod Mod => ModContent.GetInstance<ModImpl>();

    [ModSystemHooks.ResizeArrays]
    private static void ResizeArrays()
    {
        blocksItemPickupsWhenHeld = CreateSet(nameof(blocksItemPickupsWhenHeld), false);

        return;

        static T[] CreateSet<T>(string name, T defaultState)
        {
            return ItemID.Sets.Factory.CreateNamedSet(Mod, name)
                         .RegisterCustomSet(defaultState);
        }
    }

    extension(ItemID.Sets)
    {
        public static bool[] BlocksItemPickupsWhenHeld => blocksItemPickupsWhenHeld;
    }

    [OnLoad]
    private static void Load()
    {
        On_Player.GrabItems += GrabItems_BlocksItemPickupsWhenHeld;
    }

    private static void GrabItems_BlocksItemPickupsWhenHeld(On_Player.orig_GrabItems orig, Player self, int i)
    {
        if (ItemID.Sets.BlocksItemPickupsWhenHeld[self.HeldItem.type])
        {
            return;
        }

        orig(self, i);
    }
}
