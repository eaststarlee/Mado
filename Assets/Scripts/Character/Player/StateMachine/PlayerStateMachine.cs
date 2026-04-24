// 플레이어의 상태를 관리하고 전환하는 클래스
public class PlayerStateMachine
{
    // 현재 활성화된 상태
    public PlayerState CurrentState { get; private set; }
    public PlayerState PreviousState { get; private set; }

    // 상태 머신 초기화 (첫 상태를 설정)
    public void Initialize(PlayerState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    // 다른 상태로 전환
    public void ChangeState(PlayerState newState)
    {
        // 현재 상태를 나가고
        CurrentState.Exit();
        // 이전 상태를 기록
        PreviousState = CurrentState;
        // 새로운 상태로 교체 후
        CurrentState = newState;
        // 새로운 상태에 진입
        CurrentState.Enter();
    }
}
