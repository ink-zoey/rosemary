using System;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace Rosemary.Common;

public static class ModContentExtensions
{
    extension(ModContent)
    {
        public static object GetInstance(Type type)
        {
            return ContentInstance.contentByType[type].instance;
        }

        public static T GetInstanceAs<T>(Type type) where T : class
        {
            return Unsafe.As<T>(ModContent.GetInstance(type));
        }
    }
}
