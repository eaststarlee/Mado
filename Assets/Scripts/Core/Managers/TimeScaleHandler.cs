using UnityEngine;

public class TimeScaleHandler : MonoBehaviour
{
    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState prev, GameState current)
    {
        if (current == GameState.Paused)
        {
            Time.timeScale = 0f;
        }
        else if (prev == GameState.Paused && current != GameState.Paused)
        {
            Time.timeScale = 1f;
        }
    }
}
