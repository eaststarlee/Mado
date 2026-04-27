using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Mado.Character.Animation.Editor
{
    public static class AnimToSpriteClipConverter
    {
        // Project 창에서 AnimationClip을 우클릭했을 때만 활성화되도록 검증
        [MenuItem("Assets/Animation/Convert to Sprite Clip", true)]
        private static bool ValidateConvert()
        {
            return Selection.activeObject is AnimationClip;
        }

        // 우클릭 컨텍스트 메뉴에 버튼 추가
        [MenuItem("Assets/Animation/Convert to Sprite Clip")]
        private static void ConvertSelected()
        {
            int successCount = 0;
            foreach (Object obj in Selection.objects)
            {
                if (obj is AnimationClip clip)
                {
                    if (ConvertClip(clip))
                    {
                        successCount++;
                    }
                }
            }

            if (successCount > 0)
            {
                EditorUtility.DisplayDialog("변환 완료", $"총 {successCount}개의 애니메이션 클립이 SpriteAnimationClip으로 변환되었습니다.", "확인");
            }
        }

        private static bool ConvertClip(AnimationClip clip)
        {
            // 1. 클립에서 SpriteRenderer의 sprite 속성을 변경하는 커브를 찾음
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            EditorCurveBinding spriteBinding = objectBindings.FirstOrDefault(b => b.type == typeof(SpriteRenderer) && b.propertyName == "m_Sprite");

            if (spriteBinding.propertyName == null)
            {
                Debug.LogWarning($"[AnimConverter] '{clip.name}' 클립에 Sprite 변경 키프레임이 없어 변환을 건너뜁니다.");
                return false;
            }

            // 2. 키프레임 데이터 추출
            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding);
            if (keyframes == null || keyframes.Length == 0) return false;

            // 3. 애니메이션 이벤트 추출 (소리 재생, 공격 판정 등)
            AnimationEvent[] events = clip.events;

            // 4. 새로운 SpriteAnimationClip 데이터 생성
            SpriteAnimationClip newClip = ScriptableObject.CreateInstance<SpriteAnimationClip>();
            newClip.isLoop = clip.isLooping;

            List<AnimFrame> framesList = new List<AnimFrame>();

            for (int i = 0; i < keyframes.Length; i++)
            {
                AnimFrame frame = new AnimFrame();
                frame.sprite = keyframes[i].value as Sprite;

                // Duration 계산 (현재 프레임과 다음 프레임 사이의 시간차)
                if (i < keyframes.Length - 1)
                {
                    frame.duration = keyframes[i + 1].time - keyframes[i].time;
                }
                else
                {
                    // 마지막 프레임의 노출 시간: 전체 클립 길이 - 현재 프레임 시작 시간
                    // (최소 1프레임의 시간은 보장)
                    frame.duration = Mathf.Max(clip.length - keyframes[i].time, 1f / clip.frameRate);
                }

                // 현재 프레임 구간(duration) 내에 찍혀 있는 유니티 AnimationEvent 찾기
                float frameStartTime = keyframes[i].time;
                float frameEndTime = frameStartTime + frame.duration;

                // 오차 범위를 약간 둬서 정확한 시간에 찍힌 이벤트 매핑
                foreach (var ev in events)
                {
                    if (ev.time >= frameStartTime - 0.001f && ev.time < frameEndTime - 0.001f)
                    {
                        frame.eventName = ev.functionName; // 기존 호출 함수 이름을 이벤트 문자열로 이관
                        break; // 한 프레임에 하나의 주요 이벤트만 처리
                    }
                }

                framesList.Add(frame);
            }

            newClip.frames = framesList.ToArray();

            // 5. 변환된 SO 에셋을 원본 .anim 파일과 같은 폴더에 생성 및 저장
            string path = AssetDatabase.GetAssetPath(clip);
            string directory = System.IO.Path.GetDirectoryName(path);
            string newAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{clip.name}_SpriteAnim.asset");

            AssetDatabase.CreateAsset(newClip, newAssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AnimConverter] 변환 성공: {clip.name} -> {newAssetPath}");
            return true;
        }
    }
}
