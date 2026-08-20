using Microsoft.Xna.Framework;
using Terraria;

namespace Rosemary.Common;

public static class GoreExtensions
{
    extension(Gore gore)
    {
        public Vector2 Center
        {
            get => new(gore.position.X + gore.Width * 0.5f, gore.position.Y + gore.Height * 0.5f);

            set => gore.position = new Vector2(value.X - gore.Width * 0.5f, value.Y - gore.Height * 0.5f);
        }

        public Vector2 Left
        {
            get => new(gore.position.X, gore.position.Y + gore.Height * 0.5f);

            set => gore.position = new Vector2(value.X, value.Y - gore.Height * 0.5f);
        }

        public Vector2 Right
        {
            get => new(gore.position.X + gore.Width, gore.position.Y + gore.Height * 0.5f);

            set => gore.position = new Vector2(value.X - gore.Width, value.Y - gore.Height * 0.5f);
        }

        public Vector2 Top
        {
            get => new(gore.position.X + gore.Width * 0.5f, gore.position.Y);

            set => gore.position = new Vector2(value.X - gore.Width * 0.5f, value.Y);
        }

        public Vector2 TopLeft
        {
            get => gore.position;

            set => gore.position = value;
        }

        public Vector2 TopRight
        {
            get => new(gore.position.X + gore.Width, gore.position.Y);

            set => gore.position = new Vector2(value.X - gore.Width, value.Y);
        }

        public Vector2 Bottom
        {
            get => new(gore.position.X + gore.Width * 0.5f, gore.position.Y + gore.Height);

            set => gore.position = new Vector2(value.X - gore.Width * 0.5f, value.Y - gore.Height);
        }

        public Vector2 BottomLeft
        {
            get => new(gore.position.X, gore.position.Y + gore.Height);

            set => gore.position = new Vector2(value.X, value.Y - gore.Height);
        }

        public Vector2 BottomRight
        {
            get => new(gore.position.X + gore.Width, gore.position.Y + gore.Height);

            set => gore.position = new Vector2(value.X - gore.Width, value.Y - gore.Height);
        }

        public Vector2 Size => new(gore.Width, gore.Height);

        public Rectangle Hitbox => new((int)gore.position.X, (int)gore.position.Y, (int)gore.Width, (int)gore.Height);
    }
}
