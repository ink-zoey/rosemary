using Daybreak.Common.Features.Authorship;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace Rosemary.Content;

internal abstract class CommonAuthorTag : AuthorTag
{
    private const string suffix = "Tag";

    public override string Name => base.Name.EndsWith(suffix) ? base.Name[..^suffix.Length] : base.Name;

    public override string Texture => string.Join('/', Assets.Authorship.Zoey.KEY.Split('/')[..^1]) + '/' + Name;
}

internal sealed class ZoeyTag : CommonAuthorTag
{
    private static readonly Color glow_color = new(255, 0, 0);

    public override void DrawIcon(SpriteBatch sb, Vector2 position)
    {
        var fade = MathF.Sin(Main.GlobalTimeWrappedHourly);

        var frame = new Rectangle((int)position.X, (int)position.Y - 2, 26, 26);

        var color = Color.White * (1 - fade);
        color.A = byte.MaxValue;

        sb.Draw(
            Assets.Authorship.Zoey.Asset.Value,
            frame,
            color
        );

        var glowColor = glow_color * (1 - MathF.Pow(1 - fade, 3f));
        {
            sb.Draw(
                Assets.Authorship.Zoey_Glow.Asset.Value,
                frame,
                glowColor
            );
        }
    }
}
