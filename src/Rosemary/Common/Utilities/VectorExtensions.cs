using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace Rosemary.Common;

public static class VectorExtensions
{
    extension(Vector2 vector)
    {
        // Gross but no other way of going about this really.
        public Vector2 Normalized => Vector2.Normalize(vector);
    }
}
