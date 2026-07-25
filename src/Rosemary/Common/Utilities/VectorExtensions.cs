using System;
using Microsoft.Xna.Framework;

namespace Rosemary.Common;

public static class VectorExtensions
{
    extension(Vector2 vector)
    {
        // Gross but no other way of going about this really.
        public Vector2 Normalized => Vector2.Normalize(vector);

        public Vector2 WithLength(float value)
        {
            return vector.Normalized * value;
        }

        public Vector2 Transform(Matrix matrix)
        {
            return Vector2.Transform(vector, matrix);
        }

        public Vector2 Round()
        {
            return new Vector2(MathF.Round(vector.X), MathF.Round(vector.Y));
        }

        /*
        public Vector2 Floor()
        {
            return new Vector2(MathF.Floor(vector.X), MathF.Floor(vector.Y));
        }
        */

        public Vector2 Ceiling()
        {
            return new Vector2(MathF.Ceiling(vector.X), MathF.Ceiling(vector.Y));
        }
    }

    extension(ref Vector2 vector)
    {
        public float Magnitude
        {
            get => vector.Length();

            set
            {
                var newVector = vector.Normalized * value;

                vector.X = newVector.X;
                vector.Y = newVector.Y;
            }
        }
    }
}
