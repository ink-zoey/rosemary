#include "../common.h"

sampler Texture : register(s0);

float4 RippleRedShaderFragment(float2 uv : TEXCOORD0) : COLOR0
{
    float value = tex2D(Texture, uv).x;
    
    value -= 0.5;
    value *= 2;
    
    float4 black = float4(0, 0, 0, abs(value));
    
    float4 red = float4(pow(value, 2), 0, 0, value);
    
    return black + red;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(RippleRedShader)      
        PIXEL_SHADER(compile ps_3_0 RippleRedShaderFragment())        
    END_PASS
END_TECHNIQUE
