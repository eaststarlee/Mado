using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 슬롯 버튼 하나의 순수 View 컴포넌트.
/// ─ SaveManager / BootSequencer / MainMenuUI를 전혀 알지 못합니다.
/// ─ 클릭 시 Action 이벤트만 발행하고, 구독은 MainMenuUI가 담당합니다.
/// </summary>
public class SlotButtonItem : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────
    [Header("버튼 (슬롯 클릭 전체 영역)")]
    [SerializeField] private Button selectButton;

    [Header("삭제 버튼 (저장 데이터 있을 때만 표시)")]
    [SerializeField] private Button deleteButton;

    [Header("슬롯 정보 텍스트")]
    [SerializeField] private TextMeshProUGUI slotInfoText;

    [Header("슬롯 배경 이미지")]
    [SerializeField] private Image backgroundImage;

    [Header("배경 색상")]
    [SerializeField] private Color colorNormal = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color colorEmpty  = new Color(0.10f, 0.10f, 0.10f, 1f);

    // ── 이벤트 (owner를 전혀 모름) ───────────────────────
    /// <summary>슬롯 선택 시 인덱스를 전달합니다.</summary>
    public event Action<int> OnSelected;

    /// <summary>삭제 버튼 클릭 시 인덱스를 전달합니다.</summary>
    public event Action<int> OnDeleteRequested;

    // ── 내부 상태 ────────────────────────────────────────
    private int _slotIndex;

    // ── 초기화 ──────────────────────────────────────────

    /// <summary>MainMenuUI.Refresh()에서 호출합니다.</summary>
    public void Setup(int slotIndex, SaveSlotMeta meta)
    {
        _slotIndex = slotIndex;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(HandleSelect);

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(HandleDeleteRequest);

        RefreshDisplay(meta);
    }

    // ── 표시 갱신 ────────────────────────────────────────

    private void RefreshDisplay(SaveSlotMeta meta)
    {
        if (meta.isEmpty)
        {
            // 빈 슬롯 -> "NEW GAME"
            slotInfoText.text = $"<b>SLOT {_slotIndex + 1}</b>\n"
                              + $"<color=#aaaaaa>NEW GAME</color>";
            backgroundImage.color = colorEmpty;
            deleteButton.gameObject.SetActive(false);
        }
        else
        {
            // 저장 데이터 있음 -> "CONTINUE"
            int hours   = Mathf.FloorToInt(meta.totalPlayTime / 3600f);
            int mins    = Mathf.FloorToInt((meta.totalPlayTime % 3600f) / 60f);
            string date = meta.lastSavedAt > 0
                ? DateTimeOffset.FromUnixTimeSeconds(meta.lastSavedAt)
                               .LocalDateTime.ToString("MM/dd HH:mm")
                : "--/-- --:--";

            slotInfoText.text = $"<b>SLOT {_slotIndex + 1}</b>  "
                              + $"<color=#88ff88>CONTINUE</color>\n"
                              + $"<size=13><color=#cccccc>"
                              + $"LOC: {meta.sceneName}  |  "
                              + $"PLAY: {hours:D2}:{mins:D2}  |  "
                              + $"SAVE: {date}"
                              + $"</color></size>";
            backgroundImage.color = colorNormal;
            deleteButton.gameObject.SetActive(true);
        }
    }

    // ── 이벤트 발행 ─────────────────────────────────────

    private void HandleSelect()        => OnSelected?.Invoke(_slotIndex);
    private void HandleDeleteRequest() => OnDeleteRequested?.Invoke(_slotIndex);

    // ── 외부에서 인터랙션 잠금 ─────────────────────────

    public void SetInteractable(bool interactable)
    {
        selectButton.interactable = interactable;
        if (deleteButton != null)
            deleteButton.interactable = interactable;
    }
}
