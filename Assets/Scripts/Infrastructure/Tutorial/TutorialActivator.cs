using System;
using CommonUI.Tutorial;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace EchoEdge.Infra.Tutorial
{
    public class TutorialActivator : MonoBehaviour
    {
        public static TutorialActivator Instance;

        [SerializeField]
        private TextBasePresenter _textBasePresenter;

        private void Start()
        {
            Instance = this;
        }

        public async UniTask StartTutorial(TutorialKinds kind)
        {
            await _textBasePresenter.StartTutorial(kind);
        }

        /// <summary>
        /// 指定したチュートリアルが未再生なら再生し、終わるまで待つ。再生済みなら何もしない。
        /// 再生できなかった (シーンに未配置・テキストデータのロード失敗など) 場合も、ゲームを止めずに戻る。
        /// </summary>
        /// <returns>チュートリアルを再生した場合は true。</returns>
        public static async UniTask<bool> TryStartTutorialOnce(TutorialKinds kind)
        {
            if (Instance == null) return false;
            if (!TutorialSaveManager.TryBeginTutorial(kind)) return false;

            try
            {
                await Instance.StartTutorial(kind);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"チュートリアルを中止: {e}");
            }
            return true;
        }
    }
}
