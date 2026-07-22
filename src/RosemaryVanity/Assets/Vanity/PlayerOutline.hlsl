#include "../common.h"

sampler PlayerTexture : register(s0);

float MaxScale;

float4 BaseColor;

float StepSize;

SCREEN_SIZE(ScreenSize)

float4 PlayerOutlineShaderFragment(float2 uv : TEXCOORD0, float4 input : COLOR0) : COLOR0
{
    float scale = input.a * MaxScale;
    
    float3 step = float3(StepSize / (ScreenSize * scale), 0);
    
    float center = tex2D(PlayerTexture, uv).a;
    
    if (center <= 0)
    {
        discard;
    }
    
    float left = tex2D(PlayerTexture, uv - step.xz).a;
    float right = tex2D(PlayerTexture, uv + step.xz).a;
    float up = tex2D(PlayerTexture, uv - step.zy).a;
    float down = tex2D(PlayerTexture, uv + step.zy).a;
    
    bool isOutline = (left + right + up + down) < 4;
    
    return isOutline * BaseColor;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(PlayerOutlineShader)    
        PIXEL_SHADER(compile ps_3_0 PlayerOutlineShaderFragment())     
    END_PASS
END_TECHNIQUE
