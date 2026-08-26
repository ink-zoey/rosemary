#include "../../common.h"

#define PI (3.14159265359)
#define TAU (6.28318530718)

float SpikeCount;

float Time;

float Noise(float value)
{
    return frac(sin(dot(float2(value, -value), float2(127.1, 311.7))) * 43758.5453123);
}

float4 MesmerizerShaderFragment(float2 uv : TEXCOORD0, float4 baseColor : COLOR0) : COLOR0
{
    uv -= 0.5;
    
    float angle = atan2(uv.x, uv.y);
    float dist = length(uv) * 2;
    
    float2 polar = float2(angle, dist);
    
    polar.x /= TAU;
    
    float phase = polar.x * SpikeCount;
    
    float spikes = phase;
    spikes %= 1;
    spikes -= 0.5;
    spikes = abs(spikes);
    
    float noise = Noise(floor((phase + 0.5) % SpikeCount) / SpikeCount);
    
    float height = 1 - abs(sin(Time * (4 - noise) + (noise * PI)));
    
    height *= baseColor.a * 1;
    
    spikes = step(spikes * (height), saturate(1 - polar.y));
    
    return spikes;
}

BEGIN_TECHNIQUE(Technique1)
    BEGIN_PASS(MesmerizerShader)      
        PIXEL_SHADER(compile ps_3_0 MesmerizerShaderFragment())       
    END_PASS
END_TECHNIQUE
