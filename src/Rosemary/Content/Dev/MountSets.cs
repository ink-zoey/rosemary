using Daybreak.Hooks;
using Rosemary.Content.Elk;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content.Dev;

public static class DevMountSets
{
    private static bool[] ignoresHoverFatigue = [];

    private static Mod Mod => ModContent.GetInstance<ModImpl>();

    [ModSystemHooks.ResizeArrays]
    private static void ResizeArrays()
    {
        ignoresHoverFatigue = CreateSet(nameof(ignoresHoverFatigue), false);

        return;

        static T[] CreateSet<T>(string name, T defaultState)
        {
            return MountID.Sets.Factory.CreateNamedSet(Mod, name)
                          .RegisterCustomSet(defaultState);
        }
    }

    extension(MountID.Sets)
    {
        public static bool[] IgnoresHoverFatigue => ignoresHoverFatigue;
    }

    [OnLoad]
    private static void Load()
    {
        On_Mount.DoesHoverIgnoresFatigue += DoesHoverIgnoresFatigue_IgnoresHoverFatigue;
        IL_Mount.Hover += _ => { };
        IL_Mount.TryBeginningFlight += _ => { };
    }

    private static bool DoesHoverIgnoresFatigue_IgnoresHoverFatigue(On_Mount.orig_DoesHoverIgnoresFatigue orig, Mount self)
    {
        return orig(self) || MountID.Sets.IgnoresHoverFatigue[self.Type];
    }
}
