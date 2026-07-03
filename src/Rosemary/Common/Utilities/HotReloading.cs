using Rosemary.Common;
using System;
using System.Reflection;
using System.Reflection.Metadata;
using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

#if PROJECT_BUILD && DEBUG
[assembly: MetadataUpdateHandler(typeof(HotReloading))]

namespace Rosemary.Common;

public static class HotReloading
{
#region Nasty Boilerplate
    public static void UpdateApplication(Type[]? updatedTypes)
    {
        if (updatedTypes is null)
        {
            return;
        }

        if (ModLoader.isLoading)
        {
            return;
        }

        // Because ProjectBuild is mega cursed we have to load the 'alternate' copy of our assembly from tMod.
        var mod = ModLoader.GetMod(nameof(Rosemary));

        var assembly = mod.Code;

        var invokeOnHotReloadInfo =
            assembly.GetType(typeof(HotReloading).FullName!)?
                    .GetMethod(
                         nameof(InvokeOnHotReload),
                         BindingFlags.Static | BindingFlags.NonPublic
                     );

        for (var i = 0; i < updatedTypes.Length; i++)
        {
            updatedTypes[i] = assembly.GetType(updatedTypes[i].FullName!)!;
        }

        invokeOnHotReloadInfo?.Invoke(null, [updatedTypes]);
    }

    private static event Action<Type[]>? OnHotReload; 

    private static void InvokeOnHotReload(Type[] types)
    {
        OnHotReload?.Invoke(types);
    }
#endregion

    [OnLoad]
    private static void Load()
    {
        OnHotReload += OnHotReload_SetStaticDefaults;
    }

    private static void OnHotReload_SetStaticDefaults(Type[] updatedTypes)
    {
        foreach (var type in updatedTypes)
        {
            if (!type.IsAssignableTo(typeof(ModType)))
            {
                return;
            }

            Main.NewText($"Running {nameof(ModType.SetStaticDefaults)} for type: {type.Name}...", Color.Yellow);

            ModContent.GetInstanceAs<ModType>(type).SetStaticDefaults();
        }
    }
}
#endif
