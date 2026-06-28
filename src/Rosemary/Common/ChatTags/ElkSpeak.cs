using Daybreak.Common.Features.ChatTags;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI.Chat;

namespace Rosemary.Common;

public sealed class ElkTagHandler : ILoadableTagHandler<ElkTagHandler>
{
    private sealed class ElkSnippet : TextSnippet
    {
        public ElkSnippet(string text, Color color) : base(text, color)
        {

        }

        public override bool UniqueDraw(bool justCheckingSize, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = new Vector2(), Color color = new Color(), float scale = 1)
        {
            return base.UniqueDraw(justCheckingSize, out size, spriteBatch, position, color, scale);
        }
    }

    string[] ILoadableTagHandler<ElkTagHandler>.TagNames { get; } = ["elk"];

    TextSnippet ITagHandler.Parse(string text, Color baseColor, string? options)
    {
        return new ElkSnippet(text, baseColor);
    }
}
