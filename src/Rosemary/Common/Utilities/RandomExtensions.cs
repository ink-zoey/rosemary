global using Rand = Terraria.Utilities.UnifiedRandom;

using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Terraria;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Rosemary.Common;

public static class RandomExtensions
{
    extension(Rand)
    {
        public static Rand Instance => Main.rand;

        /// <summary>
        /// Generates a random value of type <typeparamref name="T"/> between 0 (inclusive) and <paramref name="max"/> (exclusive).<br/>
        /// It will not return <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Next<T>(T max)
            where T : struct,
            System.Numerics.INumber<T>,
            IConvertible
        {
            return (T)Convert.ChangeType(Rand.Instance.Sample() * max.ToDouble(null), typeof(T));
        }

        /// <summary>
        /// Generates a random value of type <typeparamref name="T"/> between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).<br/>
        /// It will not return <paramref name="max"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Next<T>(T min, T max)
            where T : struct,
            System.Numerics.INumber<T>,
            IConvertible
        {
            return (T)Convert.ChangeType(Rand.Instance.Sample() * (max - min).ToDouble(null) + min.ToDouble(null), typeof(T));
        }

        /// <returns><see langword="true"/> <paramref name="antecedent"/> out of <paramref name="consequent"/> times.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NextBoolean(int consequent = 2, int antecedent = 1)
        {
            return Rand.Next(consequent) < antecedent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Next(Vector2 size)
        {
            size.X *= Rand.Next(1f);
            size.Y *= Rand.Next(1f);

            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 NextUnitVector(float radius = 1f)
        {
            return Rand.Instance.NextVector2Unit() * radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Next(Rectangle rect)
        {
            var position = new Vector2(Rand.Next(1f) * rect.Width, Rand.Next(1f) * rect.Height);

            position += rect.TopLeft();

            return position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextDirection()
        {
            return Rand.NextBoolean() ? 1 : -1;
        }
    }
}
