using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 폼(Form) 전환 및 FormData 관리 전담 컴포넌트.
///
/// PlayerController는 이 컴포넌트를 참조하여
/// 현재 폼 데이터(ActiveFormData)와 전환(TransformTo) API를 사용합니다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerFormManager : MonoBehaviour
{
    // ── 인스펙터 ────────────────────────────────────────────────
    [Header("폼 데이터 목록")]
    [SerializeField] private List<CharacterFormData> characterForms;

    // ── 상태 (읽기 전용 노출) ────────────────────────────────────
    public FormType         CurrentForm    { get; private set; } = FormType.Normal;
    public CharacterFormData ActiveFormData { get; private set; }

    // ── 내부 ────────────────────────────────────────────────────
    private Dictionary<FormType, CharacterFormData> formDataMap;
    private SpriteRenderer spriteRenderer;
    private Sprite         normalSprite;
    private Animator       anim;

    // ── Unity Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim           = GetComponent<Animator>();

        InitializeFormData();
    }

    // ── 초기화 ───────────────────────────────────────────────────

    /// <summary>
    /// 폼 데이터 맵을 빌드하고 초기 폼을 설정합니다.
    /// PlayerController.Awake() 이후 호출됩니다(스크립트 실행 순서 주의).
    /// </summary>
    public void InitializeFormData()
    {
        formDataMap = new Dictionary<FormType, CharacterFormData>();

        if (characterForms != null)
        {
            foreach (var formData in characterForms)
            {
                if (formData != null)
                    formDataMap[formData.formType] = formData;
            }
        }

        // 기본 스프라이트 저장
        if (spriteRenderer != null)
            normalSprite = spriteRenderer.sprite;

        // 초기 폼 적용
        CurrentForm = FormType.Normal;
        if (formDataMap.TryGetValue(CurrentForm, out var initialData))
        {
            ActiveFormData = initialData;
            ApplyAnimatorController();
        }
        else if (characterForms != null && characterForms.Count > 0)
        {
            ActiveFormData = characterForms[0]; // Fallback
        }
    }

    // ── 공개 API ────────────────────────────────────────────────

    /// <summary>
    /// 폼 변신 — 데이터, 애니메이터, 스프라이트를 교체합니다.
    /// </summary>
    public void TransformTo(FormType targetForm)
    {
        if (CurrentForm == targetForm) return;
        if (!formDataMap.TryGetValue(targetForm, out var targetData)) return;

        // 1. 데이터 교체
        CurrentForm    = targetForm;
        ActiveFormData = targetData;

        // 2. 애니메이터 교체
        ApplyAnimatorController();

        // 3. 스프라이트 교체 (1프레임 튐 방지)
        if (spriteRenderer != null && ActiveFormData.formSprite != null)
        {
            normalSprite = ActiveFormData.formSprite;
            spriteRenderer.sprite = normalSprite;
        }

        // 4. 이벤트
        PlayerEvents.RaiseFormChanged(targetForm);
    }

    /// <summary>
    /// 인덱스 기반 폼 전환 (레거시 호환용)
    /// </summary>
    public void SwitchForm(int formIndex)
    {
        if (characterForms == null || formIndex < 0 || formIndex >= characterForms.Count) return;
        ActiveFormData = characterForms[formIndex];
    }

    /// <summary>
    /// 활공 시 스프라이트 교체
    /// </summary>
    public void SetGlidingSprite(bool isGliding)
    {
        if (spriteRenderer == null) return;

        if (isGliding && ActiveFormData?.ability.glidingSprite != null)
        {
            spriteRenderer.sprite = ActiveFormData.ability.glidingSprite;
        }
        else if (!isGliding && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
    }

    // ── 헬퍼 ────────────────────────────────────────────────────

    private void ApplyAnimatorController()
    {
        if (anim != null && ActiveFormData?.animatorController != null)
        {
            anim.runtimeAnimatorController = ActiveFormData.animatorController;
        }
    }
}
