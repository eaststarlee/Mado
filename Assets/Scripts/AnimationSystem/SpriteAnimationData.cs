using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mado.AnimationSystem
{
    [Serializable]
    public struct AnimationEventData
    {
        public int frameIndex;
        public string eventName;
    }

    [CreateAssetMenu(fileName = "NewSpriteAnimationData", menuName = "Anime/SpriteAnimationData")]
    public class SpriteAnimationData : ScriptableObject
    {
        [Header("Animation Setup")]
        public string stateName; // e.g. "Idle", "Move"
        public string partId;    // e.g. "Body", "Sword", "Tail"
        public bool isLooping = true;
        public float frameDuration = 0.033f;

        [Header("Sprites")]
        public Sprite[] sprites;

        [Header("Animation Events")]
        public List<AnimationEventData> events = new List<AnimationEventData>();

        /// <summary>
        /// 파일명(Asset Name)을 분석하여 stateName과 partId를 자동으로 채웁니다.
        /// 예: Devil_Idle_Tail -> State: Idle, Part: Tail
        /// </summary>
        public void ParseNameFromAsset(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return;

            // 공백 제거 및 언더바로 분리
            string[] parts = assetName.Trim().Split('_');
            if (parts.Length >= 2)
            {
                // 맨 마지막은 파트 ID
                partId = parts[parts.Length - 1];
                // 그 바로 앞은 상태명
                stateName = parts[parts.Length - 2];
            }
            else
            {
                stateName = assetName;
                partId = "Body";
            }
        }

        public int GetTotalFrames()
        {
            return sprites != null ? sprites.Length : 0;
        }
    }
}
