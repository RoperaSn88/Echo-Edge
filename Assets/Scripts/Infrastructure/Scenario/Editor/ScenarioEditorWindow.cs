using System;
using System.Collections.Generic;
using Domain.Scenario;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Infrastructure.Scenario.Editor
{
    /// <summary>
    /// ScenarioData が持つ IScenarioEvent の配列を、具象型を選んで追加・削除・並び替えできる編集ツール。
    /// </summary>
    public class ScenarioEditorWindow : EditorWindow
    {
        private static readonly (string DisplayName, Type EventType)[] AddableEventTypes =
        {
            ("キャラクター登場", typeof(CharacterAppearEvent)),
            ("表情変更", typeof(CharacterExpressionChangeEvent)),
            ("セリフ表示", typeof(Phrase)),
        };

        private const float DeleteButtonWidth = 56f;

        private ScenarioData _scenarioData;
        private SerializedObject _serializedObject;
        private SerializedProperty _eventsProperty;
        private ReorderableList _reorderableList;
        private Vector2 _scrollPosition;
        private int _pendingRemoveIndex = -1;

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

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var (displayName, eventType) in AddableEventTypes)
                {
                    if (GUILayout.Button("＋ " + displayName))
                    {
                        AddEvent(eventType);
                    }
                }
            }

            EditorGUILayout.Space();

            if (_eventsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("上のボタンからイベントを追加してください。", MessageType.Info);
            }

            _pendingRemoveIndex = -1;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _reorderableList.DoLayoutList();
            EditorGUILayout.EndScrollView();

            if (_pendingRemoveIndex >= 0)
            {
                RemoveEvent(_pendingRemoveIndex);
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void SetScenarioData(ScenarioData scenarioData)
        {
            _scenarioData = scenarioData;
            _serializedObject = _scenarioData != null ? new SerializedObject(_scenarioData) : null;
            _eventsProperty = _serializedObject?.FindProperty("_events");
            _reorderableList = _eventsProperty != null ? CreateReorderableList(_eventsProperty) : null;
        }

        private ReorderableList CreateReorderableList(SerializedProperty eventsProperty)
        {
            var list = new ReorderableList(_serializedObject, eventsProperty, true, true, false, false)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Events"),
                elementHeightCallback = GetElementHeight,
                drawElementCallback = DrawElement,
            };
            return list;
        }

        private void AddEvent(Type eventType)
        {
            Undo.RecordObject(_scenarioData, "Add Scenario Event");
            var index = _eventsProperty.arraySize;
            _eventsProperty.InsertArrayElementAtIndex(index);
            var element = _eventsProperty.GetArrayElementAtIndex(index);
            element.managedReferenceValue = Activator.CreateInstance(eventType);
            EditorUtility.SetDirty(_scenarioData);
        }

        private void RemoveEvent(int index)
        {
            Undo.RecordObject(_scenarioData, "Remove Scenario Event");
            _eventsProperty.DeleteArrayElementAtIndex(index);
            EditorUtility.SetDirty(_scenarioData);
        }

        private float GetElementHeight(int index)
        {
            var element = _eventsProperty.GetArrayElementAtIndex(index);
            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            foreach (var child in EnumerateChildren(element))
            {
                height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            return height + EditorGUIUtility.standardVerticalSpacing;
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _eventsProperty.GetArrayElementAtIndex(index);
            rect.y += EditorGUIUtility.standardVerticalSpacing;

            var headerRect = new Rect(rect.x, rect.y, rect.width - DeleteButtonWidth - 4f, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(headerRect, $"[{index}] {GetEventDisplayName(element)}", EditorStyles.boldLabel);

            var deleteButtonRect = new Rect(rect.x + rect.width - DeleteButtonWidth, rect.y, DeleteButtonWidth, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(deleteButtonRect, "削除"))
            {
                _pendingRemoveIndex = index;
            }

            var y = rect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.indentLevel++;
            foreach (var child in EnumerateChildren(element))
            {
                var height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, height), child, true);
                y += height + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.indentLevel--;
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
                if (element.managedReferenceFullTypename.Contains(eventType.FullName ?? eventType.Name))
                {
                    return displayName;
                }
            }

            return "Unknown Event";
        }
    }
}
