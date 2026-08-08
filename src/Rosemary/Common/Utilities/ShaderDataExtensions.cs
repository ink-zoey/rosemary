using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;

namespace Rosemary.Common;

public static class ShaderDataExtensions
{
    extension(WaterShaderData)
    {
        public static WaterShaderData Instance => (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
    }
}
