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

    private void OnGUI()
    {
        if (!showDebugInfo) return;
        if (health == null || entity == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
        // Check if behind camera
        if (screenPos.z < 0) return;
        
        // Flip Y for GUI
        float guiY = Screen.height - screenPos.y;

        // Brain 상태 표시
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

        // Shadow
        GUI.color = Color.black;
        GUI.Label(new Rect(screenPos.x + 1, guiY + 1, 300, 80), debugText);

        // Text
        GUI.color = Color.white;
        GUI.Label(new Rect(screenPos.x, guiY, 300, 80), debugText);
    }
}
