using System;
using UnityEngine;

/// <summary>
/// 게임 전역 이벤트 관리
/// </summary>
public static class GameEvents
{
    /// <summary>
    /// 플레이어 사망 이벤트
    /// </summary>
    public static event Action OnPlayerDeath;
    public static void RaisePlayerDeath()
    {
        OnPlayerDeath?.Invoke();
    }
    
    /// <summary>
    /// 세이브 포인트 활성화 이벤트
    /// </summary>
    public static event Action<Transform> OnSavePointActivated;
    public static void RaiseSavePointActivated(Transform savePoint)
    {
        OnSavePointActivated?.Invoke(savePoint);
    }

    /// <summary>
    /// 룸 씬 진입 이벤트 (SceneLoader에서 발생 — 미니맵, 퀘스트, 업적 연동용)
    /// </summary>
    public static event Action<RoomData> OnRoomEntered;
    public static void RaiseRoomEntered(RoomData room)
    {
        OnRoomEntered?.Invoke(room);
    }

    /// <summary>
    /// 차원 전환 완료 이벤트 (SceneLoader에서 발생 — UI, 이펙트 연동용)
    /// </summary>
    public static event Action<WorldType> OnDimensionSwitched;
    public static void RaiseDimensionSwitched(WorldType world)
    {
        OnDimensionSwitched?.Invoke(world);
    }
}
