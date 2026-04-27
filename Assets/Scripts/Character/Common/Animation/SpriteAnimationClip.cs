using UnityEngine;
using System;

[Serializable]
public class AnimFrame
{
    [Tooltip("해당 프레임에 보여줄 스프라이트")]
    public Sprite sprite;
    
    [Tooltip("이 프레임이 유지되는 시간 (초). 기본값 0.08f (약 12fps)")]
    public float duration = 0.08f;
    
    [Tooltip("이 프레임에서 발생시킬 이벤트 이름 (예: PlayFootstep, EnableHitbox). 없으면 비워둠")]
    public string eventName;
}

[CreateAssetMenu(fileName = "NewSpriteAnim", menuName = "Anime/SpriteAnim")]
public class SpriteAnimationClip : ScriptableObject
{
    [Tooltip("애니메이션이 반복 재생되는지 여부")]
    public bool isLoop = true;
    
    [Tooltip("애니메이션 프레임 데이터")]
    public AnimFrame[] frames;
}
