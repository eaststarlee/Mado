using UnityEngine;

// 모든 펫 상태의 기반이 되는 클래스
public class PetState
{
    // 상태가 참조할 펫 컨트롤러와 상태 머신
    protected PetController pet;
    protected PetStateMachine stateMachine;

    // 클래스 이름에서 Pet과 State를 제거하여 애니메이션 상태 이름 자동 생성 (예: PetFollowState -> Follow)
    protected virtual string AnimStateName => GetType().Name.Replace("Pet", "").Replace("State", "");

    // 생성자
    public PetState(PetController pet, PetStateMachine stateMachine)
    {
        this.pet = pet;
        this.stateMachine = stateMachine;
    }

    // 상태에 진입할 때 한 번 호출되는 함수
    public virtual void Enter() 
    { 
        if (pet.Animator != null)
        {
            pet.Animator.Play(AnimStateName);
        }
    }

    // 상태를 빠져나갈 때 한 번 호출되는 함수
    public virtual void Exit() { }

    // 매 프레임 호출될 로직 업데이트 (MonoBehaviour의 Update)
    public virtual void LogicUpdate() { }

    // 매 물리 프레임 호출될 물리 업데이트 (MonoBehaviour의 FixedUpdate)
    public virtual void PhysicsUpdate() { }
}
