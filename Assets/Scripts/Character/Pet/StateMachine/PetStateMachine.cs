// 펫의 상태를 관리하고 전환하는 클래스
public class PetStateMachine
{
    // 현재 활성화된 상태
    public PetState CurrentState { get; private set; }
    public PetState PreviousState { get; private set; }

    // 상태 머신 초기화 (첫 상태를 설정)
    public void Initialize(PetState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    // 다른 상태로 전환
    public void ChangeState(PetState newState)
    {
        if (CurrentState == newState) return;
        
        // 현재 상태를 나가고
        CurrentState?.Exit();
        // 이전 상태를 기록
        PreviousState = CurrentState;
        // 새로운 상태로 교체 후
        CurrentState = newState;
        // 새로운 상태에 진입
        CurrentState.Enter();
    }
}
