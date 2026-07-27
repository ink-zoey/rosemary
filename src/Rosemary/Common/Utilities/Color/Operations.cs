using System;
using Microsoft.Xna.Framework;

namespace Rosemary.Common;

public static class ColorOperations
{
    extension(Color)
    {
        /// <summary>
        /// Raises each component of <paramref name="color"/> to the specified power as a normalized <see langword="float"/>.
        /// </summary>
        public static Color Pow(Color color, float amount)
        {
            color.R = PowComponent(color.R);
            color.G = PowComponent(color.G);
            color.B = PowComponent(color.B);
            color.A = PowComponent(color.A);

            return color;

            byte PowComponent(byte component)
            {
                return (byte)(Math.Pow((float)component / byte.MaxValue, amount) * byte.MaxValue);
            }
        }
    }
}
