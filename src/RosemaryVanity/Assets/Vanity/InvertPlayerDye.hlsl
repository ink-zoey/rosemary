#include "../common.h"
#include "../colors.h"
#include "../ArmorShaderData.h"

float Map(float value, float start1, float stop1, float start2, float stop2)
{
    value = clamp(value, start1, stop1);
    return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
}

float4 InvertPlayerDyeShaderFragment(float2 uv : TEXCOORD0, float4 baseColor : COLOR0) : COLOR0
{
    float4 base = tex2D(uImage0, uv);
    
    float gradient = ((uv.y * uImageSize0.y) - uSourceRect.y) / uSourceRect.w;
    
    float3 hsl = RGBToHSL(base.rgb);
    
    float brightness = 1 - hsl.z;
    brightness = pow(brightness * 1.2, 9);
    
    float4 color = float4(brightness, brightness, brightness, 1);
    
    color.r += pow(gradient, 2);
    
    return color * base.a * baseColor.a;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(InvertPlayerDyeShader)    
        PIXEL_SHADER(compile ps_3_0 InvertPlayerDyeShaderFragment())       
    END_PASS
END_TECHNIQUE
