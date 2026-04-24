using System;

[Flags]
public enum DetectionSource
{
    None       = 0,
    Proximity  = 1 << 0,  // 기척 감지 (등 뒤, 근처)
    Vision     = 1 << 1,  // 시야 감지 (눈으로 봄)
    Zone       = 1 << 2,  // 구역 침입 (트리거)
    Damage     = 1 << 3,  // 피격 (이벤트성)
    Alert      = 1 << 4   // 동료 알림 (군집)
}
