#include "../../common.h"
#include "../../colors.h"

sampler2D Texture : register(s0);

float Time;
float2 TargetPosition;

TEXTURE_SIZE(TextureSize, 0)

float3 ShimmerColor(float2 uv)
{
    float2 worldUv = TargetPosition + uv * TextureSize;
    worldUv /= 16;

    float3 color = HSLToRGB(float3((((worldUv.x + worldUv.y / 6) + Time / 30) / 6) % 1, 1, 0.5));
    color *= 0.5;
    return color;
}

float4 MesmerizerShimmerColorsShaderFragment(float2 uv : TEXCOORD0, float4 baseColor : COLOR0) : COLOR0
{
    uv *= TextureSize;
    {
        uv += TargetPosition % 2;
        uv = floor(uv / 2) * 2;
    }
    uv /= TextureSize;
    
    float4 color = tex2D(Texture, uv);
    color.rgb *= ShimmerColor(uv);
    color.a = 0;

    return color;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(MesmerizerShimmerColorsShader)       
        PIXEL_SHADER(compile ps_3_0 MesmerizerShimmerColorsShaderFragment())        
    END_PASS
END_TECHNIQUE
