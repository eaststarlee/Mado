using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 히트박스 디버그 시각화 (Game 뷰에서 런타임 표시)
/// Inspector 토글로 ON/OFF 가능
/// </summary>
public class HitboxDebugRenderer : MonoBehaviour
{
    public static HitboxDebugRenderer Instance { get; private set; }
    
    [Header("설정")]
    [SerializeField] private bool showHitboxes = true;
    
    [Header("색상")]
    [SerializeField] private Color playerAttackColor = new Color(1f, 0f, 0f, 0.7f); // 빨강
    [SerializeField] private Color enemyAttackColor = new Color(0f, 0.5f, 1f, 0.7f);  // 파랑
    
    private List<HitboxDebugInfo> activeHitboxes = new List<HitboxDebugInfo>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// 히트박스 등록 (플레이어 공격)
    /// </summary>
    public void RegisterPlayerHitbox(Vector2 center, Vector2 size, float duration = 0.1f)
    {
        if (!showHitboxes) return;
        
        activeHitboxes.Add(new HitboxDebugInfo
        {
            center = center,
            size = size,
            color = playerAttackColor,
            endTime = Time.time + duration
        });
    }
    
    /// <summary>
    /// 히트박스 등록 (적 공격)
    /// </summary>
    public void RegisterEnemyHitbox(Vector2 center, Vector2 size, float duration = 0.1f)
    {
        if (!showHitboxes) return;
        
        activeHitboxes.Add(new HitboxDebugInfo
        {
            center = center,
            size = size,
            color = enemyAttackColor,
            endTime = Time.time + duration
        });
    }
    
    private void Update()
    {
        // 만료된 히트박스 제거
        activeHitboxes.RemoveAll(h => Time.time > h.endTime);
    }
    
    
    /// <summary>
    /// GUI로 히트박스 렌더링 (시네머신과 무관하게 작동)
    /// </summary>
    private void OnGUI()
    {
        if (!showHitboxes || activeHitboxes.Count == 0) return;
        
        // 현재 카메라 찾기 (시네머신 포함)
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = FindFirstObjectByType<Camera>();
        }
        if (cam == null) return;
        
        foreach (var hitbox in activeHitboxes)
        {
            DrawBoxGUI(hitbox.center, hitbox.size, hitbox.color, cam);
        }
    }
    
    /// <summary>
    /// GUI로 박스 그리기 (월드 좌표 → 스크린 좌표 변환)
    /// </summary>
    private void DrawBoxGUI(Vector2 worldCenter, Vector2 worldSize, Color color, Camera cam)
    {
        // 월드 좌표를 스크린 좌표로 변환
        Vector3 screenCenter = cam.WorldToScreenPoint(new Vector3(worldCenter.x, worldCenter.y, 0));
        
        // 화면 밖이면 그리지 않음
        if (screenCenter.z < 0) return;
        
        // 월드 크기를 스크린 크기로 변환 (근사치)
        Vector3 screenTopRight = cam.WorldToScreenPoint(new Vector3(
            worldCenter.x + worldSize.x * 0.5f,
            worldCenter.y + worldSize.y * 0.5f,
            0
        ));
        
        float screenWidth = Mathf.Abs(screenTopRight.x - screenCenter.x) * 2f;
        float screenHeight = Mathf.Abs(screenTopRight.y - screenCenter.y) * 2f;
        
        // Y좌표 반전 (Unity GUI는 상단이 0)
        float guiY = Screen.height - screenCenter.y;
        
        // 박스 그리기
        Rect boxRect = new Rect(
            screenCenter.x - screenWidth * 0.5f,
            guiY - screenHeight * 0.5f,
            screenWidth,
            screenHeight
        );
        
        // 테두리 그리기 (4개 선)
        DrawGUILine(
            new Vector2(boxRect.xMin, boxRect.yMin),
            new Vector2(boxRect.xMax, boxRect.yMin),
            color, 2f
        );
        DrawGUILine(
            new Vector2(boxRect.xMax, boxRect.yMin),
            new Vector2(boxRect.xMax, boxRect.yMax),
            color, 2f
        );
        DrawGUILine(
            new Vector2(boxRect.xMax, boxRect.yMax),
            new Vector2(boxRect.xMin, boxRect.yMax),
            color, 2f
        );
        DrawGUILine(
            new Vector2(boxRect.xMin, boxRect.yMax),
            new Vector2(boxRect.xMin, boxRect.yMin),
            color, 2f
        );
    }
    
    /// <summary>
    /// GUI로 선 그리기
    /// </summary>
    private void DrawGUILine(Vector2 pointA, Vector2 pointB, Color color, float thickness)
    {
        // 이전 GUI 색상 저장
        Color savedColor = GUI.color;
        GUI.color = color;
        
        // 선의 각도와 길이 계산
        Vector2 direction = pointB - pointA;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float length = direction.magnitude;
        
        // 회전된 박스로 선 그리기
        Matrix4x4 savedMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, pointA);
        
        GUI.DrawTexture(
            new Rect(pointA.x, pointA.y - thickness * 0.5f, length, thickness),
            Texture2D.whiteTexture
        );
        
        GUI.matrix = savedMatrix;
        GUI.color = savedColor;
    }
    
    /// <summary>
    /// 박스 그리기 (4개 선)
    /// </summary>
    private void DrawBox(Vector2 center, Vector2 size, Color color)
    {
        GL.Color(color);
        
        Vector2 halfSize = size * 0.5f;
        
        Vector3 topLeft = new Vector3(center.x - halfSize.x, center.y + halfSize.y, 0);
        Vector3 topRight = new Vector3(center.x + halfSize.x, center.y + halfSize.y, 0);
        Vector3 bottomRight = new Vector3(center.x + halfSize.x, center.y - halfSize.y, 0);
        Vector3 bottomLeft = new Vector3(center.x - halfSize.x, center.y - halfSize.y, 0);
        
        // 상단
        GL.Vertex(topLeft);
        GL.Vertex(topRight);
        
        // 우측
        GL.Vertex(topRight);
        GL.Vertex(bottomRight);
        
        // 하단
        GL.Vertex(bottomRight);
        GL.Vertex(bottomLeft);
        
        // 좌측
        GL.Vertex(bottomLeft);
        GL.Vertex(topLeft);
    }
    
    /// <summary>
    /// 토글 값 동적 변경 (외부에서 접근 가능)
    /// </summary>
    public void SetShowHitboxes(bool show)
    {
        showHitboxes = show;
    }
}

/// <summary>
/// 히트박스 디버그 정보
/// </summary>
public struct HitboxDebugInfo
{
    public Vector2 center;
    public Vector2 size;
    public Color color;
    public float endTime;
}
