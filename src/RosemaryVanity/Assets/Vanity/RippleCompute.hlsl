#include "../common.h"

sampler Texture : register(s0);

float Decay;
float Strength;

float StepSize;

TEXTURE_SIZE(TextureSize, 0)

float4 RippleComputeShaderFragment(float2 uv : TEXCOORD0) : COLOR0
{
    float2 pixel = float2(StepSize / TextureSize);
    
    float2 center = tex2D(Texture, uv).xy;
    
    float left = tex2D(Texture, uv - float2(pixel.x, 0)).x;
    float right = tex2D(Texture, uv + float2(pixel.x, 0)).x;
    float up = tex2D(Texture, uv - float2(0, pixel.y)).x;
    float down = tex2D(Texture, uv + float2(0, pixel.y)).x;
    
    float d = Strength * smoothstep(0, 5, (center.y - 0.5) * 2);
    d -= (center.y - 0.5) * 2;
    d += left + right + up + down - 2.1;
    d *= Decay;
    
    d *= 0.5;
    d += 0.5;
    
    return float4(d, center.x, 0, 0);
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(RippleComputeShader)    
        PIXEL_SHADER(compile ps_3_0 RippleComputeShaderFragment())     
    END_PASS
END_TECHNIQUE
