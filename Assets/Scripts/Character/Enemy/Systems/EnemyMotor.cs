using UnityEngine;

/// <summary>
/// 이동 실행 시스템.
/// Module이 직접 Rigidbody를 만지지 않기 위한 추상화 계층.
/// Module은 Motor에 명령을 발행하고, Motor가 실제 물리를 제어한다.
/// </summary>
public class EnemyMotor
{
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform transform;
    
    /// <summary>
    /// 현재 바라보는 방향 (1 = 오른쪽, -1 = 왼쪽).
    /// </summary>
    public int FacingDirection { get; private set; } = 1;
    
    public EnemyMotor(Rigidbody2D rb, SpriteRenderer spriteRenderer, Transform transform)
    {
        this.rb = rb;
        this.spriteRenderer = spriteRenderer;
        this.transform = transform;
    }
    
    /// <summary>
    /// 속도 직접 설정.
    /// </summary>
    public void SetVelocity(Vector2 velocity)
    {
        if (rb == null) return;
        rb.linearVelocity = velocity;
    }
    
    /// <summary>
    /// X축 속도만 설정 (Y는 유지).
    /// </summary>
    public void SetVelocityX(float velocityX)
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
    }
    
    /// <summary>
    /// Y축 속도만 설정 (X는 유지).
    /// </summary>
    public void SetVelocityY(float velocityY)
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, velocityY);
    }
    
    /// <summary>
    /// 힘 추가.
    /// </summary>
    public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
    {
        if (rb == null) return;
        rb.AddForce(force, mode);
    }
    
    /// <summary>
    /// 방향 전환 + SpriteRenderer.flipX.
    /// </summary>
    public void SetFacing(int direction)
    {
        if (direction == 0) return;
        FacingDirection = direction > 0 ? 1 : -1;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = FacingDirection < 0;
        }
    }
    
    /// <summary>
    /// X축 고정 (기존 SetImmovable 대체).
    /// </summary>
    public void Freeze()
    {
        if (rb == null) return;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    }
    
    /// <summary>
    /// X축 고정 해제.
    /// </summary>
    public void Unfreeze()
    {
        if (rb == null) return;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    
    /// <summary>
    /// 즉시 정지.
    /// includeY=true면 공중 유닛처럼 제자리 정지 (Y축 속도도 0).
    /// </summary>
    public void Stop(bool includeY = false)
    {
        if (rb == null) return;
        
        float targetY = includeY ? 0f : rb.linearVelocity.y;
        rb.linearVelocity = new Vector2(0f, targetY);
    }
    
    /// <summary>
    /// 중력 계수 조절.
    /// 비행 유닛(0) <-> 피격 추락(1) 전환용.
    /// </summary>
    public void SetGravityScale(float scale)
    {
        if (rb == null) return;
        rb.gravityScale = scale;
    }
    
    /// <summary>
    /// 위치 강제 이동.
    /// </summary>
    public void TeleportTo(Vector2 position)
    {
        if (rb == null) return;
        rb.position = position;
    }
    
    /// <summary>
    /// 현재 속도 반환.
    /// </summary>
    public Vector2 GetVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }
}
