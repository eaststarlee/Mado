#ifndef Z_DISTANCE_FOG_INCLUDED
#define Z_DISTANCE_FOG_INCLUDED

// Float 정밀도 버전
void CalculateZDistanceFog_float(float3 WorldPos, float3 CameraPos, float FogStart, float FogEnd, out float OutFogFactor)
{
    float zDistance = abs(WorldPos.z - CameraPos.z);
    float fogFactor = saturate((zDistance - FogStart) / max(0.001f, (FogEnd - FogStart)));
    OutFogFactor = fogFactor;
}

// Half 정밀도 버전 (유니티 모바일/기본 precision 대응용)
void CalculateZDistanceFog_half(half3 WorldPos, half3 CameraPos, half FogStart, half FogEnd, out half OutFogFactor)
{
    half zDistance = abs(WorldPos.z - CameraPos.z);
    half fogFactor = saturate((zDistance - FogStart) / max(0.001h, (FogEnd - FogStart)));
    OutFogFactor = fogFactor;
}

#endif // Z_DISTANCE_FOG_INCLUDED
