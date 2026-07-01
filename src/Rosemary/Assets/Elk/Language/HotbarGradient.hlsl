#include "../../common.h"

sampler CharacterTexture : register(s0);

float GradientTop;
float GradientHeight;

float4 HotbarGradientShaderFragment(float2 uv : TEXCOORD0, float2 svPos : SV_POSITION, float4 baseColor : COLOR0) : COLOR0
{
    float4 character = tex2D(CharacterTexture, uv);
    
    float gradient = saturate((svPos.y - GradientTop) / GradientHeight);
    
    gradient = 1 - gradient;
    
    gradient = 1 - pow(1 - gradient, 1.5);
    
    float4 color = baseColor * gradient;
    
    return character * color;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(HotbarGradientShader)  
        PIXEL_SHADER(compile ps_3_0 HotbarGradientShaderFragment())   
    END_PASS
END_TECHNIQUE
