#include "../common.h"

sampler Texture : register(s0);

float StepSize;

TEXTURE_SIZE(TextureSize, 0)

float4 RippleRedShaderFragment(float2 uv : TEXCOORD0) : COLOR0
{
    float2 pixel = float2(StepSize / TextureSize);
    
    float center = tex2D(Texture, uv).x;
    
    float left = tex2D(Texture, uv - float2(pixel.x, 0)).x;
    float right = tex2D(Texture, uv + float2(pixel.x, 0)).x;
    float up = tex2D(Texture, uv - float2(0, pixel.y)).x;
    float down = tex2D(Texture, uv + float2(0, pixel.y)).x;
    
    float value = center + left + right + up + down;
    value /= 5;
    
    value -= 0.5;
    value *= 2;
    
    float4 black = float4(0, 0, 0, abs(value));
    
    float4 red = float4(pow(value, 2.1), 0, 0, value);
    
    return black + red;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(RippleRedShader)      
        PIXEL_SHADER(compile ps_3_0 RippleRedShaderFragment())        
    END_PASS
END_TECHNIQUE
