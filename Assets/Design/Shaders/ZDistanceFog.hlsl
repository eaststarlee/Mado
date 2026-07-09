#ifndef Z_DISTANCE_FOG_INCLUDED
#define Z_DISTANCE_FOG_INCLUDED

// 유니티 Shader Graph CBUFFER 오버라이드 버그 우회용 전역 변수
float4 _GlobalFogColor;
float _GlobalFogStart;
float _GlobalFogEnd;
float _GlobalFogPower; // 인스펙터에서 제어하는 포그 곡선 파워

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
    
    // 2D 게임 최적화: 카메라와의 거리가 아닌, 오브젝트의 절대적인 월드 Z 좌표를 사용합니다.
    // Z가 0 이하(플레이어/전경)면 포그가 적용되지 않고, Z가 클수록(배경) 포그가 짙어집니다.
    float zDepth = WorldPos.z; 
    
    // Z좌표에 따른 안개 농도 계산 (Power 곡선 적용)
    float distanceFactor = saturate((zDepth - _GlobalFogStart) / max(0.001f, (_GlobalFogEnd - _GlobalFogStart)));
    distanceFactor = pow(distanceFactor, max(0.1f, _GlobalFogPower));
    FogFactor = distanceFactor * _GlobalFogColor.a;
}

void CalculateCompleteFog_half(half3 WorldPos, out half4 FinalFogColor, out half FogFactor)
{
    FinalFogColor = (half4)_GlobalFogColor;
    
    half zDepth = (half)WorldPos.z;
    
    half distanceFactor = saturate((zDepth - (half)_GlobalFogStart) / max(0.001h, ((half)_GlobalFogEnd - (half)_GlobalFogStart)));
    distanceFactor = pow(distanceFactor, (half)max(0.1f, _GlobalFogPower));
    FogFactor = distanceFactor * ((half4)_GlobalFogColor).a;
}

// Float 정밀도 버전
void CalculateZDistanceFog_float(float3 WorldPos, float3 CameraPos, float FogStart, float FogEnd, out float OutFogFactor)
{
    float zDepth = WorldPos.z;
    float fogFactor = saturate((zDepth - FogStart) / max(0.001f, (FogEnd - FogStart)));
    OutFogFactor = pow(fogFactor, max(0.1f, _GlobalFogPower));
}

// Half 정밀도 버전 (유니티 모바일/기본 precision 대응용)
void CalculateZDistanceFog_half(half3 WorldPos, half3 CameraPos, half FogStart, half FogEnd, out half OutFogFactor)
{
    half zDepth = (half)WorldPos.z;
    half fogFactor = saturate((zDepth - FogStart) / max(0.001h, (FogEnd - FogStart)));
    OutFogFactor = pow(fogFactor, (half)max(0.1f, _GlobalFogPower));
}

#endif // Z_DISTANCE_FOG_INCLUDED
