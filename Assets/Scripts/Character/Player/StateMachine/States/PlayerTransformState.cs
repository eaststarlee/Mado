using UnityEngine;

public class PlayerTransformState : PlayerState
{
    private float transformDuration = 1f; // 변신 소요 시간
    private float transformTimer;
    private FormType targetForm;
    private PlayerState returnState; // 변신 후 돌아갈 상태
    private float originalGravity; // 변신 전 중력 저장
    
    public PlayerTransformState(PlayerController player, PlayerStateMachine stateMachine, Mado.Character.Animation.PlayerAnimType animType) : base(player, stateMachine, animType) { }
    
    /// <summary>
    /// 변신 설정
    /// </summary>
    public void SetTransform(FormType target, PlayerState returnTo)
    {
        targetForm = target;
        returnState = returnTo;
    }
    
    public override void Enter()
    {
        base.Enter();
        
        transformTimer = 0f;
        
        // 중력 저장 후 정지
        originalGravity = player.RB.gravityScale;
        player.RB.gravityScale = 0f;
        
        // 속도 0 (제자리 변신)
        player.RB.linearVelocity = Vector2.zero;
        
        // TODO: 변신 애니메이션 트리거 (애니메이터 있을 때)
        // if (animator != null)
        //     animator.SetTrigger("Transform");
    }
    
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        transformTimer += Time.deltaTime;
        
        if (transformTimer >= transformDuration)
        {
            // 변신 완료
            player.TransformTo(targetForm);
            
            // 중력 복구 (변신 전 값으로)
            player.RB.gravityScale = originalGravity;
            
            // 원래 상태로 복귀
            stateMachine.ChangeState(returnState);
        }
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // 안전장치: 중력 복구 (변신 완료/취소 모두 처리)
        if (player.ActiveFormData != null)
        {
            player.RB.gravityScale = player.ActiveFormData.gravity.scale;
        }
        else
        {
            // Fallback: 원래 저장된 중력으로 복구
            player.RB.gravityScale = originalGravity;
        }
    }
    
    /// <summary>
    /// 변신 취소 (피격 시 호출)
    /// </summary>
    public void CancelTransform()
    {
        // 중력 즉시 복구
        player.RB.gravityScale = originalGravity;
    }
}
