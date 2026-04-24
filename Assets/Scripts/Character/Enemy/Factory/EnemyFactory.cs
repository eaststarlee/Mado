using UnityEngine;

/// <summary>
/// 적 생성 팩토리. EnemyDefinition 기반으로 적을 인스턴스화.
/// 향후 오브젝트 풀링으로 확장 가능.
/// </summary>
public class EnemyFactory : MonoBehaviour
{
    [Header("적 프리팹")]
    [SerializeField] private GameObject defaultEnemyPrefab;
    
    /// <summary>
    /// 싱글턴 인스턴스 (선택적 사용).
    /// </summary>
    public static EnemyFactory Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    /// <summary>
    /// 기본 프리팹으로 적 생성.
    /// </summary>
    /// <param name="definition">적 정의 SO</param>
    /// <param name="position">생성 위치</param>
    /// <returns>생성된 EnemyEntity</returns>
    public EnemyEntity SpawnEnemy(EnemyDefinition definition, Vector2 position)
    {
        return SpawnEnemy(definition, position, defaultEnemyPrefab);
    }
    
    /// <summary>
    /// 지정 프리팹으로 적 생성.
    /// </summary>
    /// <param name="definition">적 정의 SO</param>
    /// <param name="position">생성 위치</param>
    /// <param name="prefab">사용할 프리팹</param>
    /// <returns>생성된 EnemyEntity</returns>
    public EnemyEntity SpawnEnemy(EnemyDefinition definition, Vector2 position, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[EnemyFactory] 프리팹이 null입니다!");
            return null;
        }
        
        if (definition == null)
        {
            Debug.LogError("[EnemyFactory] EnemyDefinition이 null입니다!");
            return null;
        }
        
        // 인스턴스 생성
        var instance = Instantiate(prefab, position, Quaternion.identity);
        instance.name = $"Enemy_{definition.name}";
        
        // EnemyEntity에 Definition 주입
        var entity = instance.GetComponent<EnemyEntity>();
        if (entity == null)
        {
            Debug.LogError($"[EnemyFactory] 프리팹에 EnemyEntity가 없습니다: {prefab.name}");
            Destroy(instance);
            return null;
        }
        
        // Definition 주입 (SerializeField이므로 리플렉션 사용)
        InjectDefinition(entity, definition);
        
        // VisualProfile 적용
        ApplyVisualProfile(entity, definition.VisualSettings);
        
        return entity;
    }
    
    /// <summary>
    /// Definition을 EnemyEntity에 주입.
    /// </summary>
    private void InjectDefinition(EnemyEntity entity, EnemyDefinition definition)
    {
        // SerializeField에 접근하기 위해 리플렉션 사용
        var field = typeof(EnemyEntity).GetField("definition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(entity, definition);
        }
        else
        {
            Debug.LogError("[EnemyFactory] EnemyEntity.definition 필드를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// VisualProfile 적용 (Animator Controller 등).
    /// </summary>
    private void ApplyVisualProfile(EnemyEntity entity, VisualProfile visual)
    {
        if (visual == null) return;
        
        // Animator Controller 교체
        if (visual.animatorController != null && entity.Animator != null)
        {
            entity.Animator.runtimeAnimatorController = visual.animatorController;
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
