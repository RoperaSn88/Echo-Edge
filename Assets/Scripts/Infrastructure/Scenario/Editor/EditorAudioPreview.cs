using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EchoEdge.Infra.Scenario
{
    /// <summary>
    /// エディタ上で AudioClip を試聴するためのヘルパー。
    /// Unity には AudioClip をエディタ内で再生する公開 API が無いため、
    /// UnityEditor 内部の AudioUtil を リフレクション経由で呼び出す。
    /// 内部 API のため Unity のバージョンによって挙動が変わる可能性があり、
    /// 呼び出しに失敗した場合は例外を投げずログ出力のみに留める。
    /// </summary>
    internal static class EditorAudioPreview
    {
        private static Type _audioUtilType;
        private static MethodInfo _playClipMethod;
        private static MethodInfo _stopAllClipsMethod;
        private static bool _initialized;

        /// <summary>
        /// 指定した AudioClip をエディタ上で再生する。
        /// </summary>
        public static void PlayClip(AudioClip clip)
        {
            if (clip == null) return;

            if (!EnsureInitialized() || _playClipMethod == null)
            {
                Debug.LogWarning("この Unity バージョンでは SE の試聴機能を利用できません。");
                return;
            }

            try
            {
                StopAllClips();

                var parameters = _playClipMethod.GetParameters();
                var args = new object[parameters.Length];
                args[0] = clip;
                for (var i = 1; i < parameters.Length; i++)
                {
                    args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : GetDefaultValue(parameters[i].ParameterType);
                }

                _playClipMethod.Invoke(null, args);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SE のプレビュー再生に失敗しました: {e.Message}");
            }
        }

        /// <summary>
        /// 再生中のプレビュークリップをすべて停止する。
        /// </summary>
        public static void StopAllClips()
        {
            if (!EnsureInitialized() || _stopAllClipsMethod == null) return;

            try
            {
                _stopAllClipsMethod.Invoke(null, null);
            }
            catch
            {
                // プレビュー停止の失敗は無視する。
            }
        }

        private static bool EnsureInitialized()
        {
            if (_initialized) return _audioUtilType != null;

            _initialized = true;
            _audioUtilType = typeof(AssetDatabase).Assembly.GetType("UnityEditor.AudioUtil");
            if (_audioUtilType == null) return false;

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
            _playClipMethod = _audioUtilType.GetMethod("PlayPreviewClip", flags)
                               ?? _audioUtilType.GetMethod("PlayClip", flags);
            _stopAllClipsMethod = _audioUtilType.GetMethod("StopAllPreviewClips", flags)
                                  ?? _audioUtilType.GetMethod("StopAllClips", flags);

            return true;
        }

        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
