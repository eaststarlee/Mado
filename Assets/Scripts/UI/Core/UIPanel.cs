using UnityEngine;
using UnityEngine.EventSystems;

public class UIPanel : MonoBehaviour
{
    [Tooltip("패널 활성화 시 처음 포커스를 받을 UI 오브젝트")]
    public GameObject firstSelectedObject;

    public virtual void Show()
    {
        // 패널이 열릴 때 항상 최상단(Canvas 내에서)으로 오도록 보장
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        OnGainFocus();
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    public virtual void OnGainFocus()
    {
        if (firstSelectedObject != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedObject);
        }
    }

    public virtual void OnLostFocus()
    {
        // 포커스를 잃었을 때 처리할 내용 (예: 시각적 피드백)
    }
}
