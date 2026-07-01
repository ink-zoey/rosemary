using Rosemary.Common;
using System;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

#if DEBUG
[assembly: MetadataUpdateHandler(typeof(HotReloading))]

namespace Rosemary.Common;

public static class HotReloading
{
    public static void UpdateApplication(Type[]? updatedTypes)
    {
        if (updatedTypes is null)
        {
            return;
        }

        OnHotReload_SetStaticDefaults();
    }

    private static void OnHotReload_SetStaticDefaults()
    {
        Main.NewText($"Running '{nameof(ModType.SetStaticDefaults)}.'", Color.Gray);

        try
        {
            // ModContent.GetInstance<T> seems to break here?
            var mod = ModLoader.GetMod(nameof(Rosemary));

            LoaderUtils.ForEachAndAggregateExceptions(
                mod.GetContent<ModType>(),
                e =>
                {
                    e.SetStaticDefaults();
                }
            );
        }
        catch (Exception e)
        {
            Main.NewText(e.Message, Color.Red);
        }
    }
}
#endif
