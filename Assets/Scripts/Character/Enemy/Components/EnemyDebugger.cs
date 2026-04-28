using UnityEngine;

public class EnemyDebugger : MonoBehaviour
{
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showPoiseBar = true;
    [SerializeField] private bool showColliders = false;

    private EnemyHealth health;
    private EnemyEntity entity;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        entity = GetComponent<EnemyEntity>();
    }

    private void OnDrawGizmos()
    {
        if (health == null) return;

        // Poise Bar
        if (showPoiseBar)
        {
            DrawPoiseBar();
        }

        // Collider
        if (showColliders)
        {
            DrawColliders();
        }

#if UNITY_EDITOR
        // [Scene View 전용] 디버그 텍스트 표시
        if (showDebugInfo && entity != null)
        {
            string movModule = entity.Brain?.CurrentMovement?.ModuleName ?? "None";
            string movStatus = entity.Brain?.CurrentMovement?.GetStatus();
            if (!string.IsNullOrEmpty(movStatus)) movModule += $" ({movStatus})";

            string actModule = entity.Brain?.CurrentAction?.ModuleName ?? "None";
            string actStatus = entity.Brain?.CurrentAction?.GetStatus();
            if (!string.IsNullOrEmpty(actStatus)) actModule += $" ({actStatus})";

            string intModule = entity.Brain?.CurrentInterrupt?.ModuleName ?? "-";
            
            string stateInfo;
            if (entity.Brain != null && entity.Brain.IsInterrupted)
            {
                stateInfo = $"[INT] {intModule}";
            }
            else
            {
                stateInfo = $"Mov:{movModule} | Act:{actModule}";
            }

            string debugText = $"HP: {health.CurrentHealth:F0}/{health.MaxHealth:F0}\n" +
                               $"Poise: {health.CurrentPoise:F0}/{health.MaxPoise:F0}\n" +
                               $"State: {stateInfo}\n" +
                               $"Recovering: {(health.IsRecoveringPoise ? "Yes" : "No")}";

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, debugText, style);
        }
#endif
    }

    private void DrawPoiseBar()
    {
        Vector3 barPos = transform.position + Vector3.up * 2f;
        float barWidth = 1f;
        float barHeight = 0.1f;

        // Background
        Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        Gizmos.DrawCube(barPos, new Vector3(barWidth, barHeight, 0));

        // Fill
        if (health.MaxPoise > 0)
        {
            float poisePercent = Mathf.Clamp01(health.CurrentPoise / health.MaxPoise);
            Color poiseColor = health.IsRecoveringPoise ? Color.green : Color.yellow;
            Gizmos.color = poiseColor;

            float fillWidthRaw = barWidth * poisePercent;
            Vector3 centerPos = barPos - Vector3.right * (barWidth - fillWidthRaw) * 0.5f;
            Gizmos.DrawCube(centerPos, new Vector3(fillWidthRaw, barHeight, 0));
        }

        // Border
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(barPos, new Vector3(barWidth, barHeight, 0));
    }

    private void DrawColliders()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
