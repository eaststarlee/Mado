#if false // TODO: Unused Script (주석 처리됨)
using UnityEngine;
using Unity.Cinemachine;

// Cinemachine Impulse를 사용하여 카메라 흔들림을 제어하는 싱글톤 클래스
[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }

    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource == null)
        {
            Debug.LogError("CameraShaker: CinemachineImpulseSource 컴포넌트가 필요합니다. 스크립트가 비활성화됩니다.");
            enabled = false;
        }
    }

    /// <summary>
    /// 인스펙터에 설정된 기본값으로 카메라를 흔듭니다.
    /// </summary>
    public void ShakeDefault()
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
    }
    
    /// <summary>
    /// 지정된 속도로 카메라를 흔듭니다.
    /// </summary>
    /// <param name="velocity">흔들림의 방향과 강도</param>
    public void ShakeWithVelocity(Vector3 velocity)
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulseWithVelocity(velocity);
        }
    }

    /// <summary>
    /// 지정된 강도로 카메라를 흔듭니다.
    /// </summary>
    /// <param name="force">흔들림의 강도</param>
    public void ShakeWithForce(float force)
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulseWithForce(force);
        }
    }
}
#endif
