#include "../common.h"

sampler Texture : register(s0);

float4 RippleNegativeShaderFragment(float2 uv : TEXCOORD0) : COLOR0
{
    float value = tex2D(Texture, uv).x;
    
    value -= 0.5;
    value *= 2;
    
    return value;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(RippleNegativeShader)     
        PIXEL_SHADER(compile ps_3_0 RippleNegativeShaderFragment())       
    END_PASS
END_TECHNIQUE
