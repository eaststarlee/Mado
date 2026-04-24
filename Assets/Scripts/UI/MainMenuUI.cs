using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용한다고 가정

/// <summary>
/// 메인 메뉴 슬롯 선택 UI 컨트롤러
/// BootSequencer가 활성화하고 초기화합니다.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI 연결 (슬롯 버튼 3개)")]
    [SerializeField] private Button[] slotButtons = new Button[3];
    
    [Header("슬롯 텍스트 (각 슬롯의 내용 표시)")]
    [Tooltip("슬롯 버튼 하위에 있는 TextMeshProUGUI를 연결하세요")]
    [SerializeField] private TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[3];

    private BootSequencer bootSequencer;

    /// <summary>
    /// BootSequencer가 켜질 때 호출하여 세이브 데이터를 로드하고 화면을 갱신합니다.
    /// </summary>
    public void Initialize(BootSequencer sequencer)
    {
        bootSequencer = sequencer;

        if (SaveManager.Instance == null)
        {
            Debug.LogError("[MainMenuUI] SaveManager가 없습니다!");
            return;
        }

        // 세이브 매니저에서 3개 슬롯의 메타 데이터를 가져옵니다
        SaveSlotMeta[] metas = SaveManager.Instance.GetAllSlotMetas();

        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i; // 클로저 캡처용
            SaveSlotMeta meta = metas[i];

            // 1. 버튼 이벤트 연결
            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex, meta.isEmpty));

            // 2. 텍스트 표시
            if (meta.isEmpty)
            {
                slotTexts[i].text = $"Slot {i + 1}\n<color=#aaaaaa>새 게임</color>";
            }
            else
            {
                // 플레이 시간 (초 -> 시간:분 변환)
                int hours = Mathf.FloorToInt(meta.totalPlayTime / 3600f);
                int mins  = Mathf.FloorToInt((meta.totalPlayTime % 3600f) / 60f);
                string timeStr = $"{hours:D2}:{mins:D2}";

                // 마지막 씬 이름 가공 (선택사항)
                string sceneName = meta.sceneName;

                slotTexts[i].text = $"Slot {i + 1}\n위치: {sceneName}\n시간: {timeStr}";
            }
        }
    }

    /// <summary>
    /// 유저가 슬롯 버튼을 클릭했을 때 호출됩니다.
    /// </summary>
    private void OnSlotClicked(int slotIndex, bool isEmpty)
    {
        Debug.Log($"[MainMenuUI] 슬롯 {slotIndex} 클릭 (비어있음: {isEmpty})");

        if (isEmpty)
        {
            // 새 게임 초기화
            SaveManager.Instance.NewGame(slotIndex);
        }

        // BootSequencer에 선택을 알리고 무한 대기를 해제시킴
        bootSequencer.SelectSlotAndStart(slotIndex);
    }
}
