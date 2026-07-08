using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Xna.Framework;

namespace Rosemary.Common;

public static class MathExtensions
{
    extension(MathF)
    {
        public static float PiOver2 => MathHelper.PiOver2;

        public static float PiOver4 => MathHelper.PiOver4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max) => MathHelper.Clamp(value, min, max);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float start, float end, float interpolator) => MathHelper.Lerp(start, end, interpolator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Saturate(float value) => MathHelper.Clamp(value, 0f, 1f);
    }
}
