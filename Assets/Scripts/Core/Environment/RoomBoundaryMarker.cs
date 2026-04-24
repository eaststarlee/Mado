using UnityEngine;

/// <summary>
/// CameraManager가 Confiner2D에 연결할 "룸 경계 콜라이더"를 식별하는 마커 컴포넌트.
///
/// ■ 사용 방법 (둘 중 하나)
///   A) 권장: 오브젝트 이름을 "RoomBoundary" 로 설정 — CameraManager가 이름으로 탐색
///   B) 폴백: 오브젝트 이름이 "RoomBoundary"가 아닐 경우 이 컴포넌트를 부착
///
/// ■ 배치
///   - 룸 씬 내 RoomCamera 오브젝트의 하위 또는 동급에 배치
///   - Collider2D (PolygonCollider2D 또는 BoxCollider2D) 와 함께 사용
///   - Is Trigger: false (카메라 경계용이므로 물리 충돌 불필요, 단 Cinemachine이 Collider2D만 참조)
///
/// ■ 에디터 Gizmo
///   선택 시 경계 영역을 초록색 와이어프레임으로 표시합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomBoundaryMarker : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0.4f, 0.5f);

        if (col is BoxCollider2D box)
        {
            Gizmos.DrawWireCube(
                (Vector2)transform.position + box.offset,
                box.size
            );
        }
        else if (col is PolygonCollider2D poly)
        {
            var points = poly.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = (Vector2)transform.position + points[i];
                Vector2 b = (Vector2)transform.position + points[(i + 1) % points.Length];
                Gizmos.DrawLine(a, b);
            }
        }

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            "[RoomBoundary]\nConfiner2D 경계"
        );
    }
#endif
}
