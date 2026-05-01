using UnityEngine;

namespace CommonUI.Tutorial
{
    /// <summary>
    /// チュートリアルを起動させるための、ラッパークラス
    /// </summary>
    public class TutorialStarter: MonoBehaviour
    {
        [SerializeField, Tooltip("チュートリアルの管理クラス")]
        private TextBasePresenter _tutorialPresenter;

        private async void Start()
        {
            await Awaitable.WaitForSecondsAsync(1f);
            _tutorialPresenter.StartTutorial();
        }
    }
}