using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Mado.AnimationSystem.Editor
{
    // 파일명 속 숫자의 자릿수를 보정해 크기순으로 자연스럽게 정렬하기 위한 비교기
    public class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // 문자열 속 모든 연속된 숫자를 찾아서 앞자리에 0을 붙여 10자리 고정 폭으로 패딩합니다.
            // 예: "idle_2" -> "idle_0000000002", "idle_10" -> "idle_0000000010"
            // 이렇게 변환된 임시 문자열끼리 비교하면 자연스러운 숫자 크기순 정렬이 보장됩니다.
            string newX = Regex.Replace(x, @"\d+", m => m.Value.PadLeft(10, '0'));
            string newY = Regex.Replace(y, @"\d+", m => m.Value.PadLeft(10, '0'));

            return string.Compare(newX, newY, StringComparison.OrdinalIgnoreCase);
        }
    }

    [CustomEditor(typeof(SpriteAnimationData))]
    public class SpriteAnimationDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SpriteAnimationData data = (SpriteAnimationData)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("🤖 파일명 기반 매핑 헬퍼", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox($"현재 에셋 이름('{data.name}')을 기반으로 상태(State)와 파트(Part)를 자동 추출합니다.\n" +
                                        $"예시: 'Devil_Idle_Tail' -> State: 'Idle', Part: 'Tail'", MessageType.Info);
                
                if (GUILayout.Button("파일명으로 자동 파싱", GUILayout.Height(30)))
                {
                    Undo.RecordObject(data, "Parse State and Part from Filename");
                    data.ParseNameFromAsset(data.name);
                    EditorUtility.SetDirty(data);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[Animation System] '{data.name}'을 파싱함 -> State: '{data.stateName}', Part: '{data.partId}'");
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("📸 Quick Sprite Importer", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("여러 프레임 이미지를 드래그 앤 드롭하면 숫자가 자연스럽게 정렬(1->2->10)되어 순서대로 담깁니다.", MessageType.Info);

                Rect dropArea = GUILayoutUtility.GetRect(0.0f, 60.0f, GUILayout.ExpandWidth(true));
                GUI.Box(dropArea, "여기에 스프라이트 또는 텍스처를 드래그 앤 드롭하세요", EditorStyles.helpBox);

                Event evt = Event.current;
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!dropArea.Contains(evt.mousePosition))
                            break;

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            var naturalComparer = new NaturalStringComparer();

                            // 스프라이트만 필터링하여 자연수 크기 순 정렬
                            Sprite[] droppedSprites = DragAndDrop.objectReferences
                                .OfType<Sprite>()
                                .OrderBy(s => s.name, naturalComparer)
                                .ToArray();

                            // Texture2D가 드래그된 경우, 그 안에 내포된 스프라이트들을 전부 로드하여 자연수 순 정렬
                            if (droppedSprites.Length == 0)
                            {
                                droppedSprites = DragAndDrop.objectReferences
                                    .OfType<Texture2D>()
                                    .SelectMany(t => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(t)).OfType<Sprite>())
                                    .OrderBy(s => s.name, naturalComparer)
                                    .ToArray();
                            }

                            if (droppedSprites.Length > 0)
                            {
                                Undo.RecordObject(data, "Import Sprites");
                                data.sprites = droppedSprites;
                                EditorUtility.SetDirty(data);
                                AssetDatabase.SaveAssets();
                                Debug.Log($"[Animation System] {droppedSprites.Length}개의 스프라이트가 자연 정렬 정렬되어 '{data.name}'에 등록되었습니다.");
                            }
                        }
                        Event.current.Use();
                        break;
                }
            }
            EditorGUILayout.EndVertical();
        }
    }
}
