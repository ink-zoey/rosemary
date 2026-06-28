using Daybreak.Common.Features.ChatTags;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace Rosemary.Common;

public sealed class ElkTagHandler : ILoadableTagHandler<ElkTagHandler>
{
    private sealed class ElkSnippet(string text, Color color) : TextSnippet(text, color)
    {
        public override bool UniqueDraw(
            bool justCheckingSize,
            out Vector2 size,
            SpriteBatch sb,
            Vector2 position = new(),
            Color color = new(),
            float scale = 1
        )
        {
            size = new Vector2(40, Text.Length * 20);

            var dims = new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);

            color = color.MultiplyRGB(Color);

            sb.Draw(TextureAssets.MagicPixel.Value, dims, color);

            return true;
        }
    }

    string[] ILoadableTagHandler<ElkTagHandler>.TagNames { get; } = ["elk"];

    TextSnippet ITagHandler.Parse(string text, Color baseColor, string? options)
    {
        return new ElkSnippet(text, baseColor);
    }
}
