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
/// <summary>슬롯이 선택되어 게임 시작 준비가 완료되면 발행합니다.</summary>
public class MainMenuUI : UIPanel
{
    // ── Inspector — 패널 ─────────────────────────────────
    [Header("패널 (화면 전환)")]
    [Tooltip("타이틀 / 게임시작 / 나가기 버튼이 있는 첫 화면")]
    [SerializeField] private GameObject mainPanel;

    [Tooltip("슬롯 3개가 있는 두 번째 화면")]
    [SerializeField] private GameObject slotPanel;

    [Header("설정")]
    [SerializeField] private UIPanel settingsPanel;

    // ── Inspector — 메인 패널 버튼 ───────────────────────
    [Header("메인 패널 버튼")]
    [SerializeField] private Button gameStartButton;
    [SerializeField] private Button settingsButton;
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
        if (settingsButton != null)
            settingsButton.onClick.AddListener(HandleSettingsClicked);
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

        // 시작 시 상태 유지 — BootSequencer가 제어하지만 초기 깜빡임 방지를 위해 스스로 끄지 않음
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
        // UIManager에 다른 패널(설정 등)이 떠있다면 여기서의 ESC 처리는 양보합니다.
        if (UIManager.Instance != null && UIManager.Instance.PanelCount > 1) return;

        // ESC 키 입력 처리
        if (InputRouter.Instance != null && InputRouter.Instance.Actions.Permanent.Pause.WasPressedThisFrame())
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

    public override void OnGainFocus()
    {
        base.OnGainFocus();
        // 포커스를 얻었을 때 이전에 열려있던 패널(메인 또는 슬롯)을 다시 켭니다.
        if (slotPanelWasActive)
        {
            mainPanel.SetActive(false);
            slotPanel.SetActive(true);
        }
        else
        {
            mainPanel.SetActive(true);
            slotPanel.SetActive(false);
        }
    }

    public override void OnLostFocus()
    {
        base.OnLostFocus();
        // 설정창 등이 뜰 때 메인 메뉴 콘텐츠를 가려서 겹쳐 보이지 않게 합니다.
        mainPanel.SetActive(false);
        slotPanel.SetActive(false);
    }

    private bool slotPanelWasActive = false;

    // ── 공개 API (BootSequencer가 호출) ─────────────────

    /// <summary>메뉴 전체를 활성화하고 메인 패널부터 표시합니다.</summary>
    public override void Show()
    {
        base.Show();
        ShowMainPanel();
    }

    /// <summary>메뉴 전체를 비활성화합니다.</summary>
    public override void Hide()
    {
        base.Hide();
    }

    // ── 패널 전환 ────────────────────────────────────────

    private void ShowMainPanel()
    {
        slotPanelWasActive = false;
        mainPanel.SetActive(true);
        slotPanel.SetActive(false);
        SetInteractable(true);
    }

    private void ShowSlotPanel()
    {
        slotPanelWasActive = true;
        mainPanel.SetActive(false);
        slotPanel.SetActive(true);
        Refresh();
    }

    /// <summary>SaveManager에서 메타를 다시 읽어 슬롯 표시를 갱신합니다.</summary>
    public void Refresh()
    {
        SaveSlotMeta[] metas = SaveSystem.GetAllSlotMetas();
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
        SetInteractable(false);

        // 빈 슬롯이면 LoadSlot이 알아서 빈 데이터를 만듦
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.LoadSlot(slotIndex);
        }

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
            SaveSystem.DeleteSlot(slotIndex);
            Refresh();
        });
    }

    // ── 나가기 ───────────────────────────────────────────

    private void HandleSettingsClicked()
    {
        if (settingsPanel != null)
        {
            UIManager.Instance.PushPanel(settingsPanel);
        }
        else
        {
            Debug.LogWarning("[MainMenuUI] SettingsPanel이 연결되지 않았습니다.");
        }
    }

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
