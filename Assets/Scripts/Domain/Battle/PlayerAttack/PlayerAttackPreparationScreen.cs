using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Domain.Battle.PlayerAttack
{
    public class PlayerAttackPreparationScreen : MonoBehaviour
    {
        [SerializeField]
        private PlayerAttackPreparationViewController _viewController;

        /// <summary>
        /// シーン上のインスタンスへ他のスクリプト（<see cref="PlayerAttackPreparationPhase"/>など）から
        /// 参照するためのプロパティ。
        /// </summary>
        public static PlayerAttackPreparationScreen Instance { get; private set; }

        private readonly PlayerAttackPreparationScreenModel _screenModel = new();

        /// <summary>
        /// 攻撃準備に関する状態を保持するScreenModel。入力の切り分け（切り替え・決定の判断）は
        /// <see cref="PlayerAttackPreparationPhase"/> 側で行い、その結果をこのScreenModel経由で反映する。
        /// </summary>
        public PlayerAttackPreparationScreenModel ScreenModel => _screenModel;

        private void Awake()
        {
            Instance = this;
        }

        public async UniTask InitializeAsync(CancellationToken token)
        {
            await _screenModel.InitializeAsync();
            _viewController.Initialize(_screenModel.PlayerAttackPreparationViewModel);
            await _viewController.InitializeAsync(token);
        }

        public async UniTask OnShowAsync(CancellationToken token)
        {
            await _viewController.ShowAsync(token);
        }

        public async UniTask OnHideAsync(CancellationToken token)
        {
            await _viewController.HideAsync(token);
        }
    }
}
