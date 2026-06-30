#include "../../common.h"
#include "../../colors.h"

sampler CharacterTexture : register(s0);

float GradientTop;
float GradientHeight;

float4 GradientColor;

float4 RarityGradientShaderFragment(float2 uv : TEXCOORD0, float2 svPos : SV_POSITION, float4 baseColor : COLOR0) : COLOR0
{
    float4 character = tex2D(CharacterTexture, uv);
    
    float gradient = (svPos.y - GradientTop) / GradientHeight;
    
    gradient = 1 - gradient;
    
    gradient = 1 - pow(1 - gradient, 1.5);
    
    float4 color = oklabLerp(baseColor, GradientColor, gradient);
    
    return character * color;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(RarityGradientShader) 
        PIXEL_SHADER(compile ps_3_0 RarityGradientShaderFragment())  
    END_PASS
END_TECHNIQUE
