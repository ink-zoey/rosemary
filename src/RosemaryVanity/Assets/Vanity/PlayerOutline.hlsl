#include "../common.h"

sampler PlayerTexture : register(s0);

#define THRESHOLD (0.5)

float MaxScale;

float4 BaseColor;

float StepSize;

SCREEN_SIZE(ScreenSize)

float4 PlayerOutlineShaderFragment(float2 uv : TEXCOORD0, float4 input : COLOR0) : COLOR0
{
    float scale = input.a * MaxScale;
    
    float2 scaledPixel = StepSize / (ScreenSize * scale);
    
    float4 step = float4(scaledPixel, -scaledPixel.y, 0);
    
    float center = tex2D(PlayerTexture, uv).a;
    
    if (center < THRESHOLD)
    {
        discard;
    }
    
    bool upLeft = tex2D(PlayerTexture, uv - step.xy).a < THRESHOLD;
    bool left = tex2D(PlayerTexture, uv - step.xw).a < THRESHOLD;
    bool downLeft = tex2D(PlayerTexture, uv - step.xz).a < THRESHOLD;
    
    bool upRight = tex2D(PlayerTexture, uv + step.xz).a < THRESHOLD;
    bool right = tex2D(PlayerTexture, uv + step.xw).a < THRESHOLD;
    bool downRight = tex2D(PlayerTexture, uv + step.xy).a < THRESHOLD;
    
    bool up = tex2D(PlayerTexture, uv - step.wy).a < THRESHOLD;
    bool down = tex2D(PlayerTexture, uv + step.wy).a < THRESHOLD;
    
    bool isOutline = upLeft || left || downLeft || upRight || right || downRight || up || down;
    
    float4 color = BaseColor * pow(1 - input.a, 1);
    
    return color * isOutline;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(PlayerOutlineShader)    
        PIXEL_SHADER(compile ps_3_0 PlayerOutlineShaderFragment())     
    END_PASS
END_TECHNIQUE
