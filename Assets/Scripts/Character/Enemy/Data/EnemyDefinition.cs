using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 적 통합 정의. 이 에셋 1개가 곧 적 1종의 완전한 정의.
/// 기존 EnemyData의 필드를 CombatProfile로 통합하고,
/// AI 설정(BrainProfile)을 추가.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyDefinition", menuName = "Enemy/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("전투 프로필 (← 기존 EnemyData)")]
    public CombatProfile CombatSettings = new CombatProfile();
    
    [Header("AI 프로필")]
    public BrainProfile BrainSettings = new BrainProfile();
    
    [Header("비주얼 프로필 (Optional)")]
    public VisualProfile VisualSettings = new VisualProfile();
    
    // --- Context Menu 프리셋 ---
    
    [ContextMenu("Preset: Walker (걸어다니는 적)")]
    void PresetWalker()
    {
        CombatSettings.maxHealth = 30f;
        CombatSettings.maxPoise = 20f;
        CombatSettings.poiseRecoveryRate = 30f;
        CombatSettings.hasSuperArmor = false;
        CombatSettings.knockbackMultiplier = 1f;
        CombatSettings.stunDuration = 0.2f;
        CombatSettings.invincibilityDuration = 0.5f;
        CombatSettings.hitStopDuration = 0.05f;
        CombatSettings.hasStunState = true;
        CombatSettings.stunStateDuration = 1.5f;
        BrainSettings.abortThreshold = 0;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    
    [ContextMenu("Preset: Heavy Enemy")]
    void PresetHeavy()
    {
        CombatSettings.maxHealth = 200f;
        CombatSettings.maxPoise = 100f;
        CombatSettings.poiseRecoveryRate = 10f;
        CombatSettings.hasSuperArmor = false;
        CombatSettings.knockbackMultiplier = 0.5f;
        CombatSettings.stunDuration = 0.3f;
        CombatSettings.hasStunState = true;
        CombatSettings.stunStateDuration = 2f;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    
    [ContextMenu("Preset: Boss (슈퍼아머)")]
    void PresetBoss()
    {
        CombatSettings.maxHealth = 500f;
        CombatSettings.maxPoise = 200f;
        CombatSettings.hasSuperArmor = true;
        CombatSettings.hasStunState = true;
        CombatSettings.stunStateDuration = 3f;
        BrainSettings.abortThreshold = 5;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Preset: Fly Enemy (비행)")]
    void PresetFly()
    {
        CombatSettings.maxHealth = 15f;
        CombatSettings.maxPoise = 10f;
        CombatSettings.hasSuperArmor = false;
        CombatSettings.knockbackMultiplier = 1.2f; // 잘 밀림
        CombatSettings.stunDuration = 0.5f;
        CombatSettings.hasStunState = true;
        
        BrainSettings.abortThreshold = 0;
        
        // FlyModuleData는 별도 생성 필요
        Debug.Log("Fly Preset Applied. Please assign FlyModuleData.");
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
}

/// <summary>
/// 전투 프로필. 기존 EnemyData의 HP/포이즈/넉백 필드.
/// </summary>
[System.Serializable]
public class CombatProfile
{
    [Header("HP")]
    public float maxHealth = 100f;
    
    [Header("포이즈")]
    public float maxPoise = 50f;
    public float poiseRecoveryRate = 20f;
    public float poiseRecoveryDelay = 2f;
    public bool hasSuperArmor = false;
    public PoiseDamageTable poiseDamageTable = new PoiseDamageTable();
    
    [Header("피격 반응")]
    public float invincibilityDuration = 0.5f;
    public float hitStopDuration = 0.05f;
    
    [Header("넉백")]
    public float knockbackMultiplier = 1f;
    public AnimationCurve knockbackCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    [Header("경직 (Hit 지속시간)")]
    public float stunDuration = 0.2f;
    
    [Header("스턴 (Poise Break)")]
    public bool hasStunState = true;
    public float stunStateDuration = 1.5f;
}

/// <summary>
/// AI 프로필. 모듈 목록/Selector/Abort 설정.
/// </summary>
[System.Serializable]
public class BrainProfile
{
    [Header("Abort")]
    [Tooltip("높으면 중단이 어려움 (보스 패턴 보호), 낮으면 반응적 AI")]
    public int abortThreshold = 0;
    
    [Header("Movement")]
    [UnityEngine.Serialization.FormerlySerializedAs("crawlModuleData")]
    public WalkModuleData walkModuleData;
    
    public FlyModuleData flyModuleData; // [New]
    
    [Header("Combat")]
    public MeleeSwingModuleData meleeSwingModuleData;
    
    [Header("Movement - Dash")]
    public DashModuleData dashModuleData;
    
    [Header("페이즈 전환")]
    [Tooltip("HP 비율 임계값 배열. 예: [0.7, 0.3] → HP 70% 이하 Phase1, 30% 이하 Phase2")]
    public float[] phaseHealthThresholds;
}

/// <summary>
/// 비주얼 프로필. (Optional)
/// </summary>
[System.Serializable]
public class VisualProfile
{
    public RuntimeAnimatorController animatorController;
    public GameObject deathEffect;
}

/// <summary>
/// HitType별 기본 포이즈 데미지 테이블.
/// </summary>
[System.Serializable]
public class PoiseDamageTable
{
    public float lightDamage = 10f;
    public float heavyDamage = 30f;
    public float specialDamage = 50f;
    
    public float GetPoiseDamage(HitType hitType)
    {
        switch (hitType)
        {
            case HitType.Light:   return lightDamage;
            case HitType.Heavy:   return heavyDamage;
            case HitType.Special: return specialDamage;
            case HitType.Grab:    return float.MaxValue; // 즉시 브레이크
            default:              return lightDamage;
        }
    }
}
