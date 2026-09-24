using System.Threading;
using Cysharp.Threading.Tasks;
using EchoEdge.Domain.Phase;
using UnityEngine;

namespace EchoEdge.Presenter.UI
{
    public class OperateInfos : MonoBehaviour
    {
        public static OperateInfos Instance;
        
        [SerializeField]
        private OperateInfo[] _operateInfos = new OperateInfo[6];
        
        private OperateInfo _currentOperateInfo;
        
        private CancellationTokenSource _cancellationTokenSource;

        void Awake()
        {
            Instance = this;
        }
        
        public async UniTask SetOperateInfo(PhaseKinds phase)
        {
            // 同じフェーズなら開き直さない
            if (_currentOperateInfo != null && _currentOperateInfo.PhaseKind == phase)
            {
                return;
            }

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();

                // 中断されたフェードが途中の透明度で残らないようにする
                foreach (var info in _operateInfos)
                {
                    if (info != _currentOperateInfo)
                    {
                        info.SetAlpha(0);
                    }
                }
            }
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            var previousOperateInfo = _currentOperateInfo;
            _currentOperateInfo = null;
            foreach (var info in _operateInfos)
            {
                if (info.PhaseKind == phase)
                {
                    _currentOperateInfo = info;
                    break;
                }
            }

            // 閉じると開くを同時に行う
            await UniTask.WhenAll(
                previousOperateInfo != null ? previousOperateInfo.CloseAsync(token) : UniTask.CompletedTask,
                _currentOperateInfo != null ? _currentOperateInfo.OpenAsync(token) : UniTask.CompletedTask);
        }
    }
}