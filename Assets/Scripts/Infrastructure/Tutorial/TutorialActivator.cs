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

        public async UniTask StartTutorial()
        {
            await _textBasePresenter.StartTutorial();
        }
    }
}
