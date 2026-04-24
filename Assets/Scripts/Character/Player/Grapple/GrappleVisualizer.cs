using UnityEngine;

/// <summary>
/// 그래플링 방향 화살표를 Debug.DrawLine으로 시각화하는 컴포넌트.
/// GrappleDetector와 같은 GameObject에 부착합니다.
/// 추후 LineRenderer 교체 시 이 파일만 수정.
/// </summary>
[RequireComponent(typeof(GrappleDetector))]
public class GrappleVisualizer : MonoBehaviour
{
    [SerializeField] private GrappleDetector detector;


    private void Awake()
    {
        if (detector == null)
            detector = GetComponent<GrappleDetector>();
    }

    private void Update()
    {
        // 8방향 직관적 조준으로 변경되어 기존 방향 화살표(Debug.DrawLine 등) 제거
    }
}
