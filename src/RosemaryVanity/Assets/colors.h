#ifndef _COLORS_H
#define _COLORS_H

static const float3x3 kCONEtoLMS = float3x3(
         0.4121656120, 0.2118591070, 0.0883097947,
         0.5362752080, 0.6807189584, 0.2818474174,
         0.0514575653, 0.1074065790, 0.6302613616);
    
static const float3x3 kLMStoCONE = float3x3(
         4.0767245293, -1.2681437731, -.0041119885,
        -3.3072168827, 2.6093323231, -.7034763098,
         0.2307590544, -.3411344290, 1.7068625689);

float3 RGBToOklab(float3 rgb)
{
    return pow(mul(kCONEtoLMS, rgb), 0.33333);
}

float3 OklabToRGB(float3 oklab)
{
    return mul(kLMStoCONE, pow(oklab, 3));
}

float4 OklabLerp(float4 colA, float4 colB, float h)
{
    float3 lmsA = RGBToOklab(colA.rgb);
    float3 lmsB = RGBToOklab(colB.rgb);
    
    float3 lms = lerp(lmsA, lmsB, h);
    
    return float4(OklabToRGB(lms), lerp(colA.a, colB.a, h));
}

#define COLORS_EPSILON (1e-10)

float3 RGBToHCV(float3 color)
{
    float4 p = color.g < color.b
        ? float4(color.bg, -1, 0.6666)
        : float4(color.gb, 0, -0.3333);
    
    float4 q = color.r < p.x
        ? float4(p.xyw, color.r)
        : float4(color.r, p.yzx);
    
    float c = q.x - min(q.w, q.y);
    
    float hue = abs((q.w - q.y) / (6 * c + COLORS_EPSILON) + q.z);
    
    return float3(hue, c, q.x);
}

float3 RGBToHSL(float3 color)
{
    float3 hcv = RGBToHCV(color);
    
    float l = hcv.z - hcv.y * 0.5;
    float s = hcv.y / (1 - abs((l * 2) - 1) + COLORS_EPSILON);
    
    return float3(hcv.x, s, l);
}

float3 HueToRGB(float hue)
{
    float r = abs(hue * 6 - 3) - 1;
    float g = 2 - abs(hue * 6 - 2);
    float b = 2 - abs(hue * 6 - 4);

    return saturate(float3(r, g, b));
}

float3 HSLToRGB(float3 hsl)
{
    float3 rgb = HueToRGB(hsl.x);
    float c = (1 - abs(2 * hsl.z - 1)) * hsl.y;
    return (rgb - 0.5) * c + hsl.z;
}

#endif // _COLORS_H