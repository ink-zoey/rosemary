#include "../common.h"

sampler Texture : register(s0);

#define THRESHOLD (0.5)

float StepSize;

SCREEN_SIZE(ScreenSize)

float4 TransformStarOutlineShaderFragment(float2 uv : TEXCOORD0, float4 baseColor : COLOR0) : COLOR0
{
    float2 scaledPixel = StepSize / ScreenSize;
    
    uv = round(uv * (ScreenSize / StepSize)) / (ScreenSize / StepSize);
    
    float4 step = float4(scaledPixel, -scaledPixel.y, 0);
    
    float center = tex2D(Texture, uv).a;
    
    if (center < THRESHOLD)
    {
        discard;
    }
    
    bool upLeft = tex2D(Texture, uv - step.xy).a < THRESHOLD;
    bool left = tex2D(Texture, uv - step.xw).a < THRESHOLD;
    bool downLeft = tex2D(Texture, uv - step.xz).a < THRESHOLD;
    
    bool upRight = tex2D(Texture, uv + step.xz).a < THRESHOLD;
    bool right = tex2D(Texture, uv + step.xw).a < THRESHOLD;
    bool downRight = tex2D(Texture, uv + step.xy).a < THRESHOLD;
    
    bool up = tex2D(Texture, uv - step.wy).a < THRESHOLD;
    bool down = tex2D(Texture, uv + step.wy).a < THRESHOLD;
    
    bool isOutline = upLeft || left || downLeft || upRight || right || downRight || up || down;
    
    float4 color = baseColor;
    
    return color * isOutline;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(TransformStarOutlineShader)     
        PIXEL_SHADER(compile ps_3_0 TransformStarOutlineShaderFragment())      
    END_PASS
END_TECHNIQUE
