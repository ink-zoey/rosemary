#include "../common.h"

sampler PlayerTexture : register(s0);

float PlayerTop;

float PlayerBottom;

SCREEN_SIZE(PlayerSize)

#define EPSILON (1e-10)

float3 RGBtoHCV(float3 color)
{
    float4 p = color.g < color.b
        ? float4(color.bg, -1, 0.6666)
        : float4(color.gb, 0, -0.3333);
    
    float4 q = color.r < p.x
        ? float4(p.xyw, color.r)
        : float4(color.r, p.yzx);
    
    float c = q.x - min(q.w, q.y);
    
    float hue = abs((q.w - q.y) / (6 * c + EPSILON) + q.z);
    
    return float3(hue, c, q.x);
}

float3 RGBtoHSL(float3 color)
{
    float3 hcv = RGBtoHCV(color);
    
    float l = hcv.z - hcv.y * 0.5;
    float s = hcv.y / (1 - abs((l * 2) - 1) + EPSILON);
    
    return float3(hcv.x, s, l);
}

float Map(float value, float start1, float stop1, float start2, float stop2)
{
    value = clamp(value, start1, stop1);
    return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
}

float4 InvertPlayerShaderFragment(float2 uv : TEXCOORD0) : COLOR0
{
    float4 base = tex2D(PlayerTexture, uv);
    
    float gradient = Map(uv.y * PlayerSize.y, PlayerTop, PlayerBottom, 0, 1);
    
    float3 hsl = RGBtoHSL(base.rgb);
    
    float brightness = 1 - hsl.z;
    brightness = pow(brightness * 1.2, 9);
    
    float4 color = float4(brightness, brightness, brightness, 1);
    
    color.r += pow(gradient, 2);
    
    return color * base.a;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(InvertPlayerShader)   
        PIXEL_SHADER(compile ps_3_0 InvertPlayerShaderFragment())    
    END_PASS
END_TECHNIQUE
