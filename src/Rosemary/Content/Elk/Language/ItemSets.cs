using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content;

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
}
