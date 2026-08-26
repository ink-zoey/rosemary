#include "../../common.h"
#include "../../colors.h"

sampler2D Texture : register(s0);

float2 TargetPosition;

TEXTURE_SIZE(TextureSize, 0)

float4 MesmerizerInsetShaderFragment(float2 uv : TEXCOORD0, float4 baseColor : COLOR0) : COLOR0
{
    uv *= TextureSize;
    {
        uv += TargetPosition % 2;
        uv = floor(uv / 2) * 2;
    }
    uv /= TextureSize;
    
    float2 pixel = 2 / TextureSize;
    
    float4 step = float4(pixel, -pixel.y, 0);
    
    bool upLeft = tex2D(Texture, uv - step.xy).a < 0.5;
    bool left = tex2D(Texture, uv - step.xw).a < 0.5;
    bool downLeft = tex2D(Texture, uv - step.xz).a < 0.5;
    
    bool upRight = tex2D(Texture, uv + step.xz).a < 0.5;
    bool right = tex2D(Texture, uv + step.xw).a < 0.5;
    bool downRight = tex2D(Texture, uv + step.xy).a < 0.5;
    
    bool up = tex2D(Texture, uv - step.wy).a < 0.5;
    bool down = tex2D(Texture, uv + step.wy).a < 0.5;
    
    float alpha = 1 - upLeft - left - downLeft - upRight - right - downRight - up - down;
    alpha = saturate(alpha);
    
    float4 color = tex2D(Texture, uv) * alpha;

    return color;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(MesmerizerInsetShader)        
        PIXEL_SHADER(compile ps_3_0 MesmerizerInsetShaderFragment())         
    END_PASS
END_TECHNIQUE
