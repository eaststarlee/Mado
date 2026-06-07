using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mado.AnimationSystem
{
    [Serializable]
    public struct PartRendererMapping
    {
        public string partId;
        public SpriteRenderer renderer;
    }

    public class CharacterSpriteAnimator : MonoBehaviour
    {
        [Header("Renderers Mapping")]
        public List<PartRendererMapping> partRenderers = new List<PartRendererMapping>();

        // 각 파트별 런타임 재생 상태를 추적하기 위한 내부 클래스
        private class PartPlayState
        {
            public string partId;
            public SpriteRenderer renderer;
            public SpriteAnimationData currentData;
            
            public int currentFrameIndex;
            public float timer;
            public bool isCompleted;

            public PartPlayState(string partId, SpriteRenderer renderer)
            {
                this.partId = partId;
                this.renderer = renderer;
            }
        }

        private List<PartPlayState> _partStates = new List<PartPlayState>();
        private Dictionary<string, PartPlayState> _partStateDict = new Dictionary<string, PartPlayState>();

        private CharacterAnimationSet _currentAnimationSet;
        private string _currentStateName;

        private IAnimationEventListener[] _eventListeners;

        private void Awake()
        {
            InitializePartStates();
            ResolveEventListeners();
        }

        private void InitializePartStates()
        {
            _partStates.Clear();
            _partStateDict.Clear();

            foreach (var mapping in partRenderers)
            {
                if (!string.IsNullOrEmpty(mapping.partId) && mapping.renderer != null)
                {
                    var state = new PartPlayState(mapping.partId, mapping.renderer);
                    _partStates.Add(state);
                    _partStateDict[mapping.partId] = state;
                }
            }
        }

        private void ResolveEventListeners()
        {
            _eventListeners = GetComponentsInParent<IAnimationEventListener>();
            if (_eventListeners.Length == 0)
            {
                _eventListeners = GetComponentsInChildren<IAnimationEventListener>();
            }
        }

        public void SetAnimationSet(CharacterAnimationSet newSet)
        {
            _currentAnimationSet = newSet;
            if (_currentAnimationSet != null)
            {
                _currentAnimationSet.Initialize(true); // 강제 딕셔너리 빌드
            }
            
            // 변신 등으로 세트가 바뀌면 현재 상태의 애니메이션 데이터도 바로 교체 시도
            if (!string.IsNullOrEmpty(_currentStateName))
            {
                Play(_currentStateName, true);
            }
        }

        public void Play(string stateName, bool forceRestart = false)
        {
            if (!forceRestart && _currentStateName == stateName)
                return; // 이미 재생 중인 동일 상태 무시

            if (_currentAnimationSet == null)
                return;

            _currentStateName = stateName;

            foreach (var partState in _partStates)
            {
                SpriteAnimationData data = _currentAnimationSet.GetAnimationData(stateName, partState.partId);

                // 이번 상태에 이 파트가 없는 경우
                if (data == null)
                {
                    partState.currentData = null;
                    partState.renderer.sprite = null;
                    partState.currentFrameIndex = 0;
                    partState.timer = 0f;
                    partState.isCompleted = true;
                    continue;
                }

                // 새로운 애니메이션 데이터 할당 및 재생 초기화
                if (forceRestart || partState.currentData != data)
                {
                    partState.currentData = data;
                    partState.currentFrameIndex = 0;
                    partState.timer = 0f;
                    partState.isCompleted = false;

                    ApplyFrame(partState);
                    TriggerEventsForFrame(partState, 0);
                }
            }
        }

        private void Update()
        {
            foreach (var partState in _partStates)
            {
                if (partState.currentData == null || partState.isCompleted) 
                    continue;

                partState.timer += Time.deltaTime;

                if (partState.timer >= partState.currentData.frameDuration)
                {
                    partState.timer -= partState.currentData.frameDuration; // 프레임 오버플로우 누적 보존
                    AdvanceFrame(partState);
                }
            }
        }

        private void AdvanceFrame(PartPlayState partState)
        {
            int totalFrames = partState.currentData.GetTotalFrames();
            if (totalFrames == 0) return;

            partState.currentFrameIndex++;

            if (partState.currentFrameIndex >= totalFrames)
            {
                if (partState.currentData.isLooping)
                {
                    partState.currentFrameIndex = 0;
                }
                else
                {
                    partState.currentFrameIndex = totalFrames - 1;
                    partState.isCompleted = true;
                    return; // 마지막 프레임 고정 후 탈출
                }
            }

            ApplyFrame(partState);
            TriggerEventsForFrame(partState, partState.currentFrameIndex);
        }

        private void ApplyFrame(PartPlayState partState)
        {
            var data = partState.currentData;
            if (data == null || data.sprites == null || data.sprites.Length == 0)
            {
                partState.renderer.sprite = null;
                return;
            }

            int idx = Mathf.Clamp(partState.currentFrameIndex, 0, data.sprites.Length - 1);
            partState.renderer.sprite = data.sprites[idx];
        }

        private void TriggerEventsForFrame(PartPlayState partState, int frameIndex)
        {
            var data = partState.currentData;
            if (data == null || data.events == null) return;

            foreach (var evt in data.events)
            {
                if (evt.frameIndex == frameIndex)
                {
                    FireEvent(evt.eventName);
                }
            }
        }

        private void FireEvent(string eventName)
        {
            if (_eventListeners != null)
            {
                foreach (var listener in _eventListeners)
                {
                    listener.OnAnimationEvent(eventName);
                }
            }
        }
    }
}
