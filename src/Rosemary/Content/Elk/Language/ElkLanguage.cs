using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace Rosemary.Content;

public sealed class ElkPhrase : List<ElkSymbol>
{
    public Vector2 Measure(float scale)
    {
        var height = this.Sum(symbol => symbol.Height);

        return new Vector2(ElkSymbols.TOTAL_WIDTH, height) * scale;
    }
}

public record struct ElkSymbol(Vector2 Position, Rectangle Source, float Height);

public static class ElkLanguage
{
    public static ElkPhrase NewPhrase => [];

    extension(ElkPhrase phrase)
    {
        public ElkPhrase UseOffset(Vector2 offset)
        {
            var symbol = phrase[^1];

            symbol.Position += offset;

            phrase[^1] = symbol;

            return phrase;
        }

        public ElkPhrase UseHeight(float height = 0)
        {
            var symbol = phrase[^1];

            symbol.Height = height;

            phrase[^1] = symbol;

            return phrase;
        }
    }

    public static void DrawPhrase(this SpriteBatch sb, ElkPhrase phrase, Vector2 position, Color color, float scale, Vector2 origin)
    {
        var texture = Assets.Elk.Language.ElkAtlas.Asset.Value;

        position -= origin;

        foreach (var symbol in phrase)
        {
            var source = symbol.Source;

            source.X += 1;
            source.Y += 1;

            source.Width -= 1;
            source.Height -= 1;

            sb.Draw(
                texture,
                position + (symbol.Position * scale),
                source,
                color,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );

            position.Y += symbol.Height * scale;
        }
    }

    public static void DrawPhraseOutline(this SpriteBatch sb, ElkPhrase phrase, Vector2 position, Color color, float scale, Vector2 origin, float spread = 2f, int directions = 4)
    {
        for (var i = 0; i < directions; i++)
        {
            var ratio = (float)i / directions;

            var offset = Vector2.UnitX.RotatedBy(MathF.Tau * ratio) * spread;

            sb.DrawPhrase(phrase, position + offset, color, scale, origin);
        }
    }

    public static void DrawPhraseWithOutline(this SpriteBatch sb, ElkPhrase phrase, Vector2 position, Color color, Color shadowColor, float scale, Vector2 origin, float spread = 2f, int directions = 4)
    {
        sb.DrawPhraseOutline(phrase, position, shadowColor, scale, origin, spread, directions);
        sb.DrawPhrase(phrase, position, color, scale, origin);
    }
}

[UsedImplicitly]
public static partial class ElkSymbols
{
    public const float TOTAL_WIDTH = 40;
}