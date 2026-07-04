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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Next<T>(T max)
            where T : struct,
            System.Numerics.INumber<T>,
            IConvertible
        {
            return (T)Convert.ChangeType(Rand.Instance.Sample() * max.ToDouble(null), typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Next<T>(T min, T max)
            where T : struct,
            System.Numerics.INumber<T>,
            IConvertible
        {
            return (T)Convert.ChangeType(Rand.Instance.Sample() * (max - min).ToDouble(null) + min.ToDouble(null), typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NextBool(int consequent = 2, int antecedent = 1)
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
            return Rand.NextBool() ? 1 : -1;
        }
    }
}
