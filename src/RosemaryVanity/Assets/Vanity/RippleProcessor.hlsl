#include "../common.h"

sampler Texture : register(s0);
sampler Mask : register(s1);

float StepSize;

TEXTURE_SIZE(TextureSize, 0)

float4 RippleRedShaderFragment(float2 uv : TEXCOORD0, float4 baseColor : COLOR0) : COLOR0
{
    float2 pixel = float2(StepSize / TextureSize);
    
    float mask = tex2D(Mask, uv).a;
    
    float center = tex2D(Texture, uv).x;
    
    float left = tex2D(Texture, uv - float2(pixel.x, 0)).x;
    float right = tex2D(Texture, uv + float2(pixel.x, 0)).x;
    float up = tex2D(Texture, uv - float2(0, pixel.y)).x;
    float down = tex2D(Texture, uv + float2(0, pixel.y)).x;
    
    float value = center + left + right + up + down;
    value /= 5;
    
    value -= 0.5;
    value *= 2;
    
    float4 black = saturate(value) + pow(saturate(-value), 6);
    
    black *= (1 - mask);
    
    black *= baseColor;
    
    // float4 red = float4(pow(value, 2.1), 0, 0, value);
    
    return black;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(RippleRedShader)      
        PIXEL_SHADER(compile ps_3_0 RippleRedShaderFragment())        
    END_PASS
END_TECHNIQUE
