using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mado.AnimationSystem
{
    [CreateAssetMenu(fileName = "NewCharacterAnimationSet", menuName = "Anime/CharacterAnimationSet")]
    public class CharacterAnimationSet : ScriptableObject
    {
        [Header("Drag and Drop Animation Data Files here")]
        public List<SpriteAnimationData> animationDatas = new List<SpriteAnimationData>();

        // StateName -> PartId -> SpriteAnimationData
        private Dictionary<string, Dictionary<string, SpriteAnimationData>> _animDict;

        public void Initialize(bool force = false)
        {
            if (_animDict != null && !force) return;

            _animDict = new Dictionary<string, Dictionary<string, SpriteAnimationData>>(StringComparer.OrdinalIgnoreCase);

            foreach (var data in animationDatas)
            {
                if (data == null) continue;

                // 혹시 상태나 파트 이름이 비어있다면 파일명 기준으로 자동 파싱 시도
                if (string.IsNullOrEmpty(data.stateName) || string.IsNullOrEmpty(data.partId))
                {
                    data.ParseNameFromAsset(data.name);
                }

                string state = data.stateName;
                string part = data.partId;

                if (!string.IsNullOrEmpty(state) && !string.IsNullOrEmpty(part))
                {
                    if (!_animDict.ContainsKey(state))
                    {
                        _animDict[state] = new Dictionary<string, SpriteAnimationData>(StringComparer.OrdinalIgnoreCase);
                    }
                    _animDict[state][part] = data;
                }
            }
        }

        public bool HasState(string stateName)
        {
            if (_animDict == null)
            {
                Initialize();
            }
            return _animDict != null && _animDict.ContainsKey(stateName);
        }

        public SpriteAnimationData GetAnimationData(string stateName, string partId)
        {
            if (_animDict == null)
            {
                Initialize();
            }

            if (_animDict != null && _animDict.TryGetValue(stateName, out var partDict))
            {
                if (partDict.TryGetValue(partId, out var data))
                {
                    return data;
                }
            }
            return null;
        }
    }
}
