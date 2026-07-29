#ifndef ARMOR_SHADER_DATA_H
#define ARMOR_SHADER_DATA_H

sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);

float3 uColor;
float uSaturation;
float3 uSecondaryColor;
float uTime;
float uOpacity;
float2 uTargetPosition;
float4 uSourceRect;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;
float2 uDrawPosition;
float uRotation;
float uDirection;
float2 uImageSize0;
float2 uImageSize1;

#endif