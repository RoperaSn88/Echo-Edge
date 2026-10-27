#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EchoEdge.Infra.Audio
{
    /// <summary>
    /// AudioClip の Inspector に、SE の enum 定義ファイルを開くボタンを表示する。
    /// AudioClip の Inspector は Unity 組み込みの AudioImporter 用のため置き換えず、ヘッダーの下に追加する。
    /// </summary>
    [InitializeOnLoad]
    internal static class SeTypeInspectorButtons
    {
        private static readonly Type[] SeTypes = { typeof(BattleSeType), typeof(PreparingSeType) };

        static SeTypeInspectorButtons()
        {
            Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
        }

        private static void OnHeaderGUI(Editor editor)
        {
            if (editor.target is not AudioImporter && editor.target is not AudioClip)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var seType in SeTypes)
                {
                    if (GUILayout.Button(seType.Name))
                    {
                        OpenScript(seType);
                    }
                }
            }
        }

        /// <summary>
        /// 型名と同じファイル名のスクリプトをエディタ(IDE)で開く
        /// </summary>
        private static void OpenScript(Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets($"{type.Name} t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != type.Name)
                {
                    continue;
                }

                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
                return;
            }

            Debug.LogWarning($"{type.Name} のスクリプトが見つかりません");
        }
    }
}
#endif
