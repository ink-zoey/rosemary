using Rosemary.Common;
using Terraria.Graphics.Shaders;

namespace Rosemary.Vanity.Content;

public sealed class EmptinessDye : AbstractDyeItem
{
    public override string Texture => Assets.Vanity.EmptinessDye.KEY;

    public override string LocalizationCategory => "Content";

    public override ShaderData Data => Assets.Vanity.InvertPlayerDye.CreateInvertPlayerDyeShader();
}
