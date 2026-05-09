using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 메인 메뉴 전체 화면 컨트롤러.
///
/// ■ 화면 흐름:
///   [메인 패널] 타이틀 + 게임시작 + 나가기
///       ↓ 게임시작 클릭
///   [슬롯 패널] 슬롯 3개 + 뒤로가기
///       ↓ 슬롯 클릭
///   게임 로드 (BootSequencer가 처리)
///
/// ■ BootSequencer를 전혀 알지 못합니다.
///   OnGameStartRequested 이벤트만 발행합니다.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ── Inspector — 패널 ─────────────────────────────────
    [Header("패널 (두 화면 전환)")]
    [Tooltip("타이틀 / 게임시작 / 나가기 버튼이 있는 첫 화면")]
    [SerializeField] private GameObject mainPanel;

    [Tooltip("슬롯 3개가 있는 두 번째 화면")]
    [SerializeField] private GameObject slotPanel;

    // ── Inspector — 메인 패널 버튼 ───────────────────────
    [Header("메인 패널 버튼")]
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button quitButton;

    // ── Inspector — 슬롯 패널 ────────────────────────────
    [Header("슬롯 패널")]
    [SerializeField] private SlotButtonItem[] slotItems = new SlotButtonItem[3];
    [SerializeField] private Button           backButton;

    [Header("삭제 확인 팝업")]
    [SerializeField] private DeleteConfirmPopup deleteConfirmPopup;

    [Header("전체 입력 차단 (CanvasGroup)")]
    [Tooltip("MainMenuCanvas 루트에 붙어 있는 CanvasGroup")]
    [SerializeField] private CanvasGroup canvasGroup;

    // ── 이벤트 (BootSequencer가 구독) ───────────────────
    /// <summary>슬롯이 선택되어 게임 시작 준비가 완료되면 발행합니다.</summary>
    public event Action<int> OnGameStartRequested;

    // ── Unity Lifecycle ──────────────────────────────────

    private void Awake()
    {
        // 메인 패널 버튼 리스너
        gameStartButton.onClick.AddListener(ShowSlotPanel);
        quitButton.onClick.AddListener(HandleQuit);

        // 슬롯 패널 뒤로가기 버튼
        if (backButton != null)
            backButton.onClick.AddListener(ShowMainPanel);

        // 슬롯 아이템 이벤트 구독 (SlotButtonItem은 this를 모름)
        for (int i = 0; i < slotItems.Length; i++)
        {
            if (slotItems[i] == null) continue;
            slotItems[i].OnSelected        += HandleSlotSelected;
            slotItems[i].OnDeleteRequested += HandleDeleteRequested;
        }

        // 시작 시 비활성 — BootSequencer가 Show()로 활성화
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < slotItems.Length; i++)
        {
            if (slotItems[i] == null) continue;
            slotItems[i].OnSelected        -= HandleSlotSelected;
            slotItems[i].OnDeleteRequested -= HandleDeleteRequested;
        }
    }

    private void Update()
    {
        // ESC 키 입력 처리
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 1. 삭제 확인 팝업이 열려있으면 팝업 닫기
            if (deleteConfirmPopup != null && deleteConfirmPopup.gameObject.activeSelf)
            {
                deleteConfirmPopup.Cancel();
            }
            // 2. 슬롯 패널이 열려있으면 메인 패널로 돌아가기
            else if (slotPanel != null && slotPanel.activeSelf)
            {
                ShowMainPanel();
            }
        }
    }

    // ── 공개 API (BootSequencer가 호출) ─────────────────

    /// <summary>메뉴 전체를 활성화하고 메인 패널부터 표시합니다.</summary>
    public void Show()
    {
        gameObject.SetActive(true);
        ShowMainPanel();
    }

    /// <summary>메뉴 전체를 비활성화합니다.</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ── 패널 전환 ────────────────────────────────────────

    private void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        slotPanel.SetActive(false);
        SetInteractable(true);
    }

    private void ShowSlotPanel()
    {
        mainPanel.SetActive(false);
        slotPanel.SetActive(true);
        Refresh();
    }

    /// <summary>SaveManager에서 메타를 다시 읽어 슬롯 표시를 갱신합니다.</summary>
    public void Refresh()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[MainMenuUI] SaveManager가 없습니다!");
            return;
        }

        SaveSlotMeta[] metas = SaveManager.Instance.GetAllSlotMetas();
        for (int i = 0; i < slotItems.Length; i++)
        {
            if (slotItems[i] != null && i < metas.Length)
                slotItems[i].Setup(i, metas[i]);
        }

        SetInteractable(true);
    }

    // ── 슬롯 이벤트 핸들러 ──────────────────────────────

    private void HandleSlotSelected(int slotIndex)
    {
        if (SaveManager.Instance == null) return;

        SetInteractable(false);

        // 빈 슬롯이면 새 게임 데이터 생성
        SaveSlotMeta[] metas = SaveManager.Instance.GetAllSlotMetas();
        if (metas[slotIndex].isEmpty)
            SaveManager.Instance.NewGame(slotIndex);

        // BootSequencer에 이벤트 전달
        OnGameStartRequested?.Invoke(slotIndex);
    }

    private void HandleDeleteRequested(int slotIndex)
    {
        if (deleteConfirmPopup == null)
        {
            Debug.LogWarning("[MainMenuUI] DeleteConfirmPopup이 연결되지 않았습니다.");
            return;
        }

        deleteConfirmPopup.Show(slotIndex, () =>
        {
            SetInteractable(false);
            SaveManager.Instance?.DeleteSlot(slotIndex);
            Refresh();
        });
    }

    // ── 나가기 ───────────────────────────────────────────

    private void HandleQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── 유틸리티 ─────────────────────────────────────────

    private void SetInteractable(bool value)
    {
        if (canvasGroup != null)
            canvasGroup.interactable = value;
    }
}
