using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 액션 피드백 전담 컴포넌트.
/// 리코일(반동), 히트스톱(역경직), 포고 바운스, 중력/낙하속도 오버라이드를 담당합니다.
///
/// 물리 처리를 위해 PlayerController 참조를 Awake에서 캐싱합니다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerActionController : MonoBehaviour
{
    // ── 상태 프로퍼티 (읽기 전용) ────────────────────────────────
    public bool IsRecoiling { get; private set; }

    // ── 내부 ────────────────────────────────────────────────────
    private PlayerController player;
    private Rigidbody2D      rb;

    private Coroutine recoilCoroutine;
    private Coroutine hitStopCoroutine;

    // Gravity / FallSpeed Override
    private float? gravityOverride;
    private float? fallSpeedClamp;

    // ── Unity Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        rb     = GetComponent<Rigidbody2D>();
    }

    // ══════════════════════════════════════════════════════════
    // Recoil (반동)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 반동 시작 (Hollow Knight Style: Stop → Push → Wait → Stop)
    /// </summary>
    public void StartRecoil(Vector2 force, float duration)
    {
        if (recoilCoroutine != null) StopCoroutine(recoilCoroutine);
        recoilCoroutine = StartCoroutine(RecoilRoutine(force, duration));
    }

    private IEnumerator RecoilRoutine(Vector2 force, float duration)
    {
        IsRecoiling = true;

        if (player.IsGrounded())
            rb.linearVelocity = Vector2.zero;
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        rb.AddForce(force, ForceMode2D.Impulse);

        yield return new WaitForSeconds(duration);

        if (player.IsGrounded())
            rb.linearVelocity = Vector2.zero;
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        IsRecoiling     = false;
        recoilCoroutine = null;
    }

    // ══════════════════════════════════════════════════════════
    // HitStop (역경직)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 역경직 시작 — Time.timeScale을 일시 정지시킵니다.
    /// </summary>
    public void StartHitStop(float duration)
    {
        if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        if (duration <= 0f) yield break;

        Time.timeScale = 0.0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;

        hitStopCoroutine = null;
    }

    // ══════════════════════════════════════════════════════════
    // Pogo Bounce (포고 점프)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 포고 반동 실행 — Velocity 직접 제어로 일관된 높이 보장
    /// </summary>
    public void PogoBounce(float bounceVelocity)
    {
        // 물리 초기화
        rb.gravityScale = player.ActiveFormData.gravity.scale;
        ClearFallSpeedClamp();

        // Y 속도 직접 설정
        Vector2 velocity = rb.linearVelocity;
        velocity.y = bounceVelocity;
        rb.linearVelocity = velocity;

        // 공중 능력 리필
        player.RefillAirAbilities();

        // InAirState 강제 전환
        if (player.StateMachine.CurrentState != player.InAirState)
            player.StateMachine.ChangeState(player.InAirState);

        player.InAirState.OnPogoJump();
    }

    // ══════════════════════════════════════════════════════════
    // Gravity / FallSpeed Override
    // ══════════════════════════════════════════════════════════

    public void RequestGravityOverride(float scale)
    {
        gravityOverride = scale;
        rb.gravityScale = scale;
    }

    public void ClearGravityOverride()
    {
        gravityOverride = null;
        rb.gravityScale = player.ActiveFormData.gravity.scale;
    }

    public void RequestFallSpeedClamp(float maxSpeed)
    {
        fallSpeedClamp = maxSpeed;
    }

    public void ClearFallSpeedClamp()
    {
        fallSpeedClamp = null;
    }

    /// <summary>
    /// 낙하 속도 Clamp 적용 — FixedUpdate 등에서 호출
    /// </summary>
    public void ApplyFallSpeedClamp()
    {
        if (fallSpeedClamp.HasValue && rb.linearVelocity.y < -fallSpeedClamp.Value)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -fallSpeedClamp.Value);
        }
    }

    /// <summary>
    /// 현재 적용될 중력 스케일 (Override 우선)
    /// </summary>
    public float EffectiveGravityScale
        => gravityOverride ?? player.ActiveFormData.gravity.scale;
}
