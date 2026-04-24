using UnityEngine;

/// <summary>
/// 플레이어가 이 구역(트리거) 내에 있을 때만 차원 전환(D키 홀드)이 가능하도록 허용하는 컴포넌트.
/// Trigger Collider2D가 달린 오브젝트에 부착하여 사용합니다.
/// </summary>
public class DimensionSwitchZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerController>(out var player))
            {
                player.IsInDimensionZone = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerController>(out var player))
            {
                player.IsInDimensionZone = false;
            }
        }
    }
}
