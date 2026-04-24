using UnityEngine;

/// <summary>
/// 지형 오브젝트(타일맵 루트 또는 일반 지형)에 부착하여
/// 레이어 대신 컴포넌트 기반으로 지형 속성을 표현합니다.
///
/// [씬 계층 구조 전제]
/// 타일맵은 CompositeCollider2D를 사용하므로,
/// SurfaceInfo는 반드시 타일맵의 '루트 오브젝트'에 부착해야 합니다.
///
/// 권장 씬 구조 예시:
///   [ClimbWall Root] ← SurfaceInfo (type=Climbable, isWall=true)
///     └ Tilemap (TilemapCollider2D + CompositeCollider2D)
///   [Ground Root]   ← SurfaceInfo (type=General, isWall=false)
///     └ Tilemap
/// </summary>
public enum SurfaceType
{
    Ground,
    Wall,
    ClimbGround,
    GrapplePoint,
    BreakableWall,
    Devil_BreakableWall,
    Spike
}

public class SurfaceInfo : MonoBehaviour
{
    [Tooltip("상세 지형 속성 (타일맵/오브젝트 루트에 부착)")]
    public SurfaceType type = SurfaceType.Ground;
}
