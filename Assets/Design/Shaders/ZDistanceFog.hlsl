#ifndef Z_DISTANCE_FOG_INCLUDED
#define Z_DISTANCE_FOG_INCLUDED

// 유니티 Shader Graph CBUFFER 오버라이드 버그 우회용 전역 변수
float4 _GlobalFogColor;
float _GlobalFogStart;
float _GlobalFogEnd;

void ReadGlobalFog_float(out float4 FogColor, out float FogStart, out float FogEnd)
{
    FogColor = _GlobalFogColor;
    FogStart = _GlobalFogStart;
    FogEnd = _GlobalFogEnd;
}

void ReadGlobalFog_half(out half4 FogColor, out half FogStart, out half FogEnd)
{
    FogColor = (half4)_GlobalFogColor;
    FogStart = (half)_GlobalFogStart;
    FogEnd = (half)_GlobalFogEnd;
}

// ==============================================================================
// [완전체 함수] 모든 버그와 서브그래프 문제를 우회하기 위해 한 번에 계산하는 함수입니다.
// ==============================================================================
void CalculateCompleteFog_float(float3 WorldPos, out float4 FinalFogColor, out float FogFactor)
{
    FinalFogColor = _GlobalFogColor;
    
    // 유니티 내장 카메라 Z값 사용 (Camera 노드 버그 원천 차단)
    float camZ = _WorldSpaceCameraPos.z; 
    float zDist = abs(WorldPos.z - camZ);
    
    // 거리 비례 안개 농도에, 매니저에서 설정한 FogColor의 알파(a)값을 곱해서 최종 농도를 결정합니다.
    float distanceFactor = saturate((zDist - _GlobalFogStart) / max(0.001f, (_GlobalFogEnd - _GlobalFogStart)));
    FogFactor = distanceFactor * _GlobalFogColor.a;
}

void CalculateCompleteFog_half(half3 WorldPos, out half4 FinalFogColor, out half FogFactor)
{
    FinalFogColor = (half4)_GlobalFogColor;
    
    half camZ = (half)_WorldSpaceCameraPos.z;
    half zDist = abs(WorldPos.z - camZ);
    
    half distanceFactor = saturate((zDist - (half)_GlobalFogStart) / max(0.001h, ((half)_GlobalFogEnd - (half)_GlobalFogStart)));
    FogFactor = distanceFactor * ((half4)_GlobalFogColor).a;
}

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
