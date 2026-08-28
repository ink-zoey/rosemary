using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace Rosemary.Common;

public static class ModContentExtensions
{
    extension(ModContent)
    {
        /// <summary>
        ///     Gets the template instance of the given type as an <see langword="object"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="instance">
        ///     The template instance of the given type as an <see langword="object"/>.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the template instance was found; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryGetInstance(Type type, [NotNullWhen(true)] out object? instance)
        {
            instance = null;

            if (!ContentInstance.contentByType.TryGetValue(type, out var entry))
            {
                return false;
            }

            instance = entry.instance;

            return true;
        }

        /// <summary>
        ///     Gets the template instance of the given type as a value of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="instance">
        ///     The template instance of the given type as a value of <typeparamref name="T"/>.
        /// </param>
        /// <inheritdoc cref="TryGetInstance"/>
        public static bool TryGetInstanceAs<T>(Type type, [NotNullWhen(true)] out T? instance)
            where T : class
        {
            instance = null;

            if (!ContentInstance.contentByType.TryGetValue(type, out var entry))
            {
                return false;
            }

            instance = Unsafe.As<T>(entry.instance);

            return true;
        }

        /// <returns>The template instance of the given type as an <see langword="object"/>.</returns>
        public static object GetInstance(Type type)
        {
            return ContentInstance.contentByType[type].instance;
        }

        /// <returns>The template instance of the given type as a value of <typeparamref name="T"/>.</returns>
        public static T GetInstanceAs<T>(Type type)
            where T : class
        {
            return Unsafe.As<T>(ModContent.GetInstance(type));
        }
    }
}
