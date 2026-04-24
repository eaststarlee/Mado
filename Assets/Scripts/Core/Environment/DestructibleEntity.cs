using UnityEngine;
using UnityEngine.Events;

public enum FormRequirement
{
    Any,
    NormalOnly,
    DevilOnly
}

/// <summary>
/// 파괴 가능한 환경 오브젝트 시스템의 코어 로직입니다.
/// 체력 또는 타격 횟수 기반으로 작동하며, 특정 폼/조건에서만 데미지를 받도록 설정할 수 있습니다.
/// </summary>
[RequireComponent(typeof(SurfaceInfo))]
public class DestructibleEntity : MonoBehaviour, IDamageable
{
    [Header("파괴 설정")]
    [Tooltip("true일 경우 데미지 수치만큼 깎이며, false일 경우 1타격당 1씩 깎입니다(횟수제).")]
    public bool useHealthPoint = false;
    
    [Tooltip("파괴에 필요한 공격 횟수 (또는 체력)")]
    public int maxHealthOrHits = 3;
    
    [Tooltip("타격 폼(Form) 조건 (기본값: Any)")]
    public FormRequirement requiredForm = FormRequirement.Any;

    [Header("이벤트")]
    [Tooltip("피격 시 발생하는 이벤트 (DestructibleFeedback 등과 연결)")]
    public UnityEvent OnHit;
    
    [Tooltip("파괴 시 발생하는 이벤트 (아이템 드랍, 사운드, 문 열림 등)")]
    public UnityEvent OnDestroyed;

    private int currentHealthOrHits;
    private bool isDestroyed = false;

    // IDamageable 구현
    public bool IsInvincible => isDestroyed;

    private void Awake()
    {
        currentHealthOrHits = maxHealthOrHits;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDestroyed) return;

        // 폼 제약 확인 (Any가 아닐 때만 검사)
        if (requiredForm != FormRequirement.Any && damageInfo.source != null)
        {
            PlayerController player = damageInfo.source.GetComponent<PlayerController>();
            if (player != null)
            {
                if (requiredForm == FormRequirement.NormalOnly && player.CurrentForm != FormType.Normal) return;
                if (requiredForm == FormRequirement.DevilOnly && player.CurrentForm != FormType.Devil) return;
            }
        }

        // 데미지 또는 횟수 차감
        if (useHealthPoint)
        {
            currentHealthOrHits -= Mathf.RoundToInt(damageInfo.damage);
        }
        else
        {
            currentHealthOrHits -= 1; // 한 대 맞을 때마다 1씩 차감
        }

        // 피격 이벤트 발생
        OnHit?.Invoke();

        // 파괴 판정
        if (currentHealthOrHits <= 0)
        {
            DestroyEntity();
        }
    }

    private void DestroyEntity()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // 파괴 이벤트 발생
        OnDestroyed?.Invoke();

        // 자기 자신을 비활성화 (기획에 따라 파괴 후 애니메이션 등이 있다면 수정 가능)
        gameObject.SetActive(false);
    }
}
