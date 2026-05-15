using UnityEngine;
using System;

public enum GameState { Gameplay, Paused, Loading, Cutscene, Dead }

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Gameplay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        var prev = CurrentState;
        CurrentState = newState;
        Debug.Log($"[GameStateManager] State changed: {prev} -> {newState}");
        GameEvents.RaiseGameStateChanged(prev, newState);
    }
}
