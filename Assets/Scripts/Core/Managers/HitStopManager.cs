using UnityEngine;

/// <summary>
/// Manages global hit stop (freeze frame) effects.
/// Uses unscaled time to ensure recovery even when Time.timeScale is 0.
/// </summary>
public class HitStopManager : MonoBehaviour
{
    private static HitStopManager instance;
    public static HitStopManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[HitStopManager]");
                instance = go.AddComponent<HitStopManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // Maximum duration for a single hitstop to prevent infinite freezing
    private const float MAX_HITSTOP = 0.3f;
    
    private float hitStopTimer;
    private float normalTimeScale = 1f;
    private bool isStopped;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Freezes the game for a short duration.
    /// Subsequent calls prolong the freeze but never exceed MAX_HITSTOP.
    /// </summary>
    /// <param name="duration">Duration in real seconds.</param>
    public void Stop(float duration)
    {
        if (!enabled) return;

        if (!isStopped)
        {
            normalTimeScale = Time.timeScale;
            if (normalTimeScale == 0) normalTimeScale = 1f; // Safety check if already 0
            
            Time.timeScale = 0f;
            isStopped = true;
        }

        // Extend duration but cap it
        hitStopTimer = Mathf.Min(Mathf.Max(hitStopTimer, duration), MAX_HITSTOP);
    }

    private void Update()
    {
        if (isStopped)
        {
            // Use unscaled delta time because timeScale is 0
            hitStopTimer -= Time.unscaledDeltaTime;

            if (hitStopTimer <= 0f)
            {
                ResumeTime();
            }
        }
    }

    private void ResumeTime()
    {
        Time.timeScale = normalTimeScale;
        hitStopTimer = 0f;
        isStopped = false;
    }

    /// <summary>
    /// Forcefully resumes time. Useful for debugging or cleanups.
    /// </summary>
    [ContextMenu("Force Resume")]
    public void ForceResume()
    {
        Time.timeScale = 1f;
        hitStopTimer = 0f;
        isStopped = false;
        Debug.LogWarning("[HitStopManager] Force resumed!");
    }

    private void OnDestroy()
    {
        // Ensure time is reset when scene changes or manager is destroyed
        if (isStopped)
        {
            Time.timeScale = normalTimeScale;
        }
    }
}
