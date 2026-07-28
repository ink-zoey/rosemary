using System;
using Daybreak.ChatTags;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Rosemary.Common;
using Terraria;
using Terraria.UI.Chat;
using Terraria.Utilities;

namespace Rosemary.Content;

public sealed class ShakyTextTag : ChatTag
{
    public const string TAG_NAME = "rsmryshk";

    public override string TagName => TAG_NAME;

    private readonly record struct Options(
        float Strength
    )
    {
        public static Options Parse(string text)
        {
            var strength = 1f;

            if (float.TryParse(text, out var value))
            {
                strength = value;
            }

            return new Options(
                strength
            );
        }
    }

    private sealed class Snippet : TextSnippet, IUniqueDrawString
    {
        private readonly Options options;

        public Snippet(Options options, string text = "") : base(text)
        {
            this.options = options;
        }

        public Snippet(Options options, string text, Color color) : base(text, color)
        {
            this.options = options;
        }

        public bool UniqueDrawString(
            SpriteBatch sb,
            DynamicSpriteFont font,
            string text,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            bool justCheckingSize,
            out Vector2 size
        )
        {
            const float max_strength = 10f;
            const float offset_divisor = 40f;
            const float time_multiplier = 0.333f;

            const float spike_frequency = 40f;
            const float spike_amplitude = 0.4f;
            const float spike_start = 0.998f;

            const float rotation_multiplier = 0.05f;

            var textSize = font.MeasureString(text) * scale;
            {
                size = textSize;
            }

            var random = new FastRandom(text.GetHashCode());

            var sum = 0f;

            var strength = MathF.Clamp(options.Strength, 0f, max_strength);

            foreach (var c in text)
            {
                DrawCharacter(c.ToString());
            }
            
            return true;

            void DrawCharacter(string character)
            {
                var curPosition = position - (origin * scale).RotatedBy(rotation);
                curPosition += new Vector2(sum * scale.X, 0f).RotatedBy(rotation);

                var characterSize = font.MeasureString(character);
                curPosition += characterSize * 0.5f;

                var timeOffset = sum / offset_divisor;

                var time = Main.GlobalTimeWrappedHourly * time_multiplier * strength;

                var spike = Utils.Remap(MathF.Sin(time * random.NextFloat(0.2f, 0.9f) + timeOffset * random.NextFloat(-3f, 2f)), spike_start, 1f, 0f, 1f);

                spike *= MathF.Sin(Main.GlobalTimeWrappedHourly * spike_frequency) * spike_amplitude * strength;

                var offset = new Vector2(0f, MathF.Sin(time + timeOffset) * strength * scale.Y + spike).RotatedBy(rotation);

                var curRotation = rotation;
                curRotation += MathF.Cos(time * random.NextFloat(1.1f, 1.4f) + timeOffset * random.NextFloat(0.3f, 3f)) * rotation_multiplier * strength;

                sb.DrawString(font, character, curPosition + offset, color, curRotation, characterSize * 0.5f, scale, SpriteEffects.None, 0f);

                sum += characterSize.X;
            }
        }

        public override Color GetVisibleColor()
        {
            return Color;
        }
    }

    public override TextSnippet Parse(string text, Color baseColor = new Color(), string? options = null)
    {
        if (string.IsNullOrEmpty(options))
        {
            return new TextSnippet(text, baseColor);
        }

        var formatting = Options.Parse(options);
        return new Snippet(formatting, text, baseColor);
    }

    public static string GenerateTag(float strength, string message)
    {
        return $"[{TAG_NAME}/{strength}:{message}]";
    }
}
