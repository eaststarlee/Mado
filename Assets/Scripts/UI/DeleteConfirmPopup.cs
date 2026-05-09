using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 삭제 확인 팝업 독립 컴포넌트.
/// ─ MainMenuUI는 Show(slotIndex, onConfirm)만 호출합니다.
/// ─ 확인/취소 처리는 이 컴포넌트가 완전히 담당합니다.
/// </summary>
public class DeleteConfirmPopup : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button          confirmButton;
    [SerializeField] private Button          cancelButton;

    // ── 내부 상태 ────────────────────────────────────────
    private Action _onConfirm;

    // ── Unity Lifecycle ──────────────────────────────────

    private void Awake()
    {
        confirmButton.onClick.AddListener(HandleConfirm);
        cancelButton.onClick.AddListener(HandleCancel);
    }

    // ── 공개 API ─────────────────────────────────────────

    /// <summary>
    /// 팝업을 표시합니다.
    /// </summary>
    /// <param name="slotIndex">삭제 대상 슬롯 인덱스 (표시용)</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 실행할 콜백</param>
    public void Show(int slotIndex, Action onConfirm)
    {
        _onConfirm    = onConfirm;
        confirmText.text = $"Delete SLOT {slotIndex + 1}?\n"
                         + "<size=12><color=#aaaaaa>This cannot be undone.</color></size>";
        gameObject.SetActive(true);
    }

    public void Cancel()
    {
        HandleCancel();
    }

    // ── 내부 핸들러 ──────────────────────────────────────

    private void HandleConfirm()
    {
        gameObject.SetActive(false);
        _onConfirm?.Invoke();
        _onConfirm = null;
    }

    private void HandleCancel()
    {
        gameObject.SetActive(false);
        _onConfirm = null;
    }
}
