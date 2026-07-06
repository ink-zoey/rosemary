using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace Rosemary.Common;

public static class ModContentExtensions
{
    extension(ModContent)
    {
        public static bool TryGetInstance(Type type, [NotNullWhen(true)] out object? obj)
        {
            obj = null;

            if (!ContentInstance.contentByType.TryGetValue(type, out var entry))
            {
                return false;
            }

            obj = entry.instance;

            return true;
        }

        public static bool TryGetInstanceAs<T>(Type type, [NotNullWhen(true)] out T? obj)
            where T : class
        {
            obj = null;

            if (!ContentInstance.contentByType.TryGetValue(type, out var entry))
            {
                return false;
            }

            obj = Unsafe.As<T>(entry.instance);

            return true;
        }

        public static object GetInstance(Type type)
        {
            return ContentInstance.contentByType[type].instance;
        }

        public static T GetInstanceAs<T>(Type type)
            where T : class
        {
            return Unsafe.As<T>(ModContent.GetInstance(type));
        }
    }
}
