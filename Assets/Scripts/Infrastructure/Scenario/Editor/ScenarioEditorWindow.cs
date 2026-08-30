using System;
using System.Collections.Generic;
using Domain.Scenario;
using UnityEditor;
using UnityEngine;

namespace Infrastructure.Scenario.Editor
{
    /// <summary>
    /// ScenarioData が持つイベント一覧を、具象型を選んで追加・削除・並び替えできる編集ツール。
    /// イベント一覧は二次元のグリッドとして表示する。
    /// 行はシナリオの時間軸を表し、同じ行（列方向）に並べたイベントは同時に実行される。
    /// </summary>
    public class ScenarioEditorWindow : EditorWindow
    {
        private static readonly (string DisplayName, Type EventType)[] AddableEventTypes =
        {
            ("キャラクター登場", typeof(CharacterAppearEvent)),
            ("表情変更", typeof(CharacterExpressionChangeEvent)),
            ("セリフ表示", typeof(Phrase)),
            ("SE再生", typeof(SePlayEvent)),
            ("背景変更", typeof(BackgroundChangeEvent)),
        };

        private const float ColumnWidth = 260f;
        private const float ColumnSpacing = 8f;

        private ScenarioData _scenarioData;
        private SerializedObject _serializedObject;
        private SerializedProperty _rowsProperty;
        private SerializedProperty _bgmProperty;
        private SerializedProperty _backgroundProperty;
        private Vector2 _scrollPosition;

        // GUILayout 中は SerializedProperty の配列構造を変更できないため、
        // 変更内容を保留しておき、描画がすべて終わった後にまとめて適用する。
        private Action _pendingAction;

        [MenuItem("Tools/Scenario/Scenario Editor")]
        private static void Open()
        {
            GetWindow<ScenarioEditorWindow>("Scenario Editor");
        }

        private void OnEnable()
        {
            if (_scenarioData != null)
            {
                SetScenarioData(_scenarioData);
            }
        }

        private void OnGUI()
        {
            var newScenarioData = (ScenarioData)EditorGUILayout.ObjectField(
                "Scenario Data", _scenarioData, typeof(ScenarioData), false);
            if (newScenarioData != _scenarioData)
            {
                SetScenarioData(newScenarioData);
            }

            if (_scenarioData == null)
            {
                EditorGUILayout.HelpBox("編集する ScenarioData アセットを割り当ててください。", MessageType.Info);
                return;
            }

            _serializedObject.Update();

            EditorGUILayout.PropertyField(_bgmProperty, new GUIContent("BGM（シナリオ開始前に再生）"));
            EditorGUILayout.PropertyField(_backgroundProperty, new GUIContent("背景（シナリオ開始前に変更）"));
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "行（Step）はシナリオの時間軸を表します。同じ行に並んだイベントは同時に実行されます。",
                MessageType.Info);
            EditorGUILayout.Space();

            _pendingAction = null;

            if (_rowsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("下のボタンから行（タイミング）を追加してください。", MessageType.Info);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (var rowIndex = 0; rowIndex < _rowsProperty.arraySize; rowIndex++)
            {
                DrawRow(rowIndex);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("＋ 行（タイミング）を追加"))
            {
                var insertIndex = _rowsProperty.arraySize;
                _pendingAction = () =>
                {
                    _rowsProperty.InsertArrayElementAtIndex(insertIndex);
                    // Unity は配列挿入時に直前の要素の内容を複製するため、新しい行は明示的に空にする。
                    var eventsProperty = _rowsProperty.GetArrayElementAtIndex(insertIndex).FindPropertyRelative("_events");
                    eventsProperty.ClearArray();
                };
            }

            if (_pendingAction != null)
            {
                Undo.RecordObject(_scenarioData, "Edit Scenario");
                _pendingAction.Invoke();
                EditorUtility.SetDirty(_scenarioData);
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void SetScenarioData(ScenarioData scenarioData)
        {
            _scenarioData = scenarioData;
            _serializedObject = _scenarioData != null ? new SerializedObject(_scenarioData) : null;
            _rowsProperty = _serializedObject?.FindProperty("_rows");
            _bgmProperty = _serializedObject?.FindProperty("_bgm");
            _backgroundProperty = _serializedObject?.FindProperty("_background");
        }

        /// <summary>
        /// 時間軸上の1行（同時に実行するイベント群）を描画する。
        /// </summary>
        private void DrawRow(int rowIndex)
        {
            var rowProperty = _rowsProperty.GetArrayElementAtIndex(rowIndex);
            var eventsProperty = rowProperty.FindPropertyRelative("_events");

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                DrawRowHeader(rowIndex, eventsProperty);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (eventsProperty.arraySize == 0)
                    {
                        EditorGUILayout.HelpBox("この行にはまだイベントがありません。上のボタンから追加してください。", MessageType.None);
                    }

                    for (var columnIndex = 0; columnIndex < eventsProperty.arraySize; columnIndex++)
                    {
                        DrawEventColumn(eventsProperty, columnIndex);
                    }
                }
            }
        }

        /// <summary>
        /// 行の見出し（Step番号、行の並び替え・削除、イベント追加ボタン）を描画する。
        /// </summary>
        private void DrawRowHeader(int rowIndex, SerializedProperty eventsProperty)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Step {rowIndex}", EditorStyles.boldLabel, GUILayout.Width(60));

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(rowIndex == 0))
                {
                    if (GUILayout.Button("▲", GUILayout.Width(24)))
                    {
                        _pendingAction = () => _rowsProperty.MoveArrayElement(rowIndex, rowIndex - 1);
                    }
                }

                using (new EditorGUI.DisabledScope(rowIndex == _rowsProperty.arraySize - 1))
                {
                    if (GUILayout.Button("▼", GUILayout.Width(24)))
                    {
                        _pendingAction = () => _rowsProperty.MoveArrayElement(rowIndex, rowIndex + 1);
                    }
                }

                if (GUILayout.Button("行を削除", GUILayout.Width(70)))
                {
                    _pendingAction = () => _rowsProperty.DeleteArrayElementAtIndex(rowIndex);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("同時に追加:", GUILayout.Width(70));
                foreach (var (displayName, eventType) in AddableEventTypes)
                {
                    if (GUILayout.Button("＋ " + displayName))
                    {
                        _pendingAction = () => AddEvent(eventsProperty, eventType);
                    }
                }
            }
        }

        /// <summary>
        /// 同じ行内の1つのイベント（列）を描画する。
        /// </summary>
        private void DrawEventColumn(SerializedProperty eventsProperty, int columnIndex)
        {
            var element = eventsProperty.GetArrayElementAtIndex(columnIndex);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(ColumnWidth)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"[{columnIndex}] {GetEventDisplayName(element)}", EditorStyles.boldLabel);

                    using (new EditorGUI.DisabledScope(columnIndex == 0))
                    {
                        if (GUILayout.Button("◀", GUILayout.Width(22)))
                        {
                            _pendingAction = () => eventsProperty.MoveArrayElement(columnIndex, columnIndex - 1);
                        }
                    }

                    using (new EditorGUI.DisabledScope(columnIndex == eventsProperty.arraySize - 1))
                    {
                        if (GUILayout.Button("▶", GUILayout.Width(22)))
                        {
                            _pendingAction = () => eventsProperty.MoveArrayElement(columnIndex, columnIndex + 1);
                        }
                    }

                    if (GUILayout.Button("削除", GUILayout.Width(40)))
                    {
                        _pendingAction = () => eventsProperty.DeleteArrayElementAtIndex(columnIndex);
                    }
                }

                EditorGUI.indentLevel++;
                foreach (var child in EnumerateChildren(element))
                {
                    EditorGUILayout.PropertyField(child, true);
                }
                EditorGUI.indentLevel--;

                if (IsEventOfType(element, typeof(SePlayEvent)))
                {
                    DrawSePreviewButton(element);
                }
            }

            GUILayout.Space(ColumnSpacing);
        }

        /// <summary>
        /// SE再生イベントに割り当てられた AudioClip をエディタ上で試聴するボタンを描画する。
        /// </summary>
        private static void DrawSePreviewButton(SerializedProperty element)
        {
            var clipProperty = element.FindPropertyRelative("_clip");
            var clip = clipProperty?.objectReferenceValue as AudioClip;

            using (new EditorGUI.DisabledScope(clip == null))
            {
                if (GUILayout.Button("▶ 試聴"))
                {
                    EditorAudioPreview.PlayClip(clip);
                }
            }
        }

        private static void AddEvent(SerializedProperty eventsProperty, Type eventType)
        {
            var index = eventsProperty.arraySize;
            eventsProperty.InsertArrayElementAtIndex(index);
            var element = eventsProperty.GetArrayElementAtIndex(index);
            element.managedReferenceValue = Activator.CreateInstance(eventType);
        }

        private static IEnumerable<SerializedProperty> EnumerateChildren(SerializedProperty parent)
        {
            var child = parent.Copy();
            var end = child.GetEndProperty();

            var hasChild = child.NextVisible(true);
            while (hasChild && !SerializedProperty.EqualContents(child, end))
            {
                yield return child.Copy();
                hasChild = child.NextVisible(false);
            }
        }

        private static string GetEventDisplayName(SerializedProperty element)
        {
            foreach (var (displayName, eventType) in AddableEventTypes)
            {
                if (IsEventOfType(element, eventType))
                {
                    return displayName;
                }
            }

            return "Unknown Event";
        }

        private static bool IsEventOfType(SerializedProperty element, Type eventType)
        {
            return element.managedReferenceFullTypename.Contains(eventType.FullName ?? eventType.Name);
        }
    }
}
