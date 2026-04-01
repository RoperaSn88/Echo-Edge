using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace UI.Energy
{
    public class EnergyPresenter: ObjectPooler
    {
        public int value;
        
        /// <summary>
        /// 終点となるUIのRectTransform
        /// </summary>
        public RectTransform rectTransform;
        
        /// <summary>
        /// 出現する位置のTransform
        /// </summary>
        public Vector3 emit;
        
        private const float AppearTime = 0.5f;
        
        public void SetPosition(RectTransform rect, Vector3 trans)
        {
            rectTransform = rect;
            emit = trans;
        }

        public async override UniTask Appear()
        {
            // rectTransformのグローバルの位置をとってくる
            // これを終点とする
            var destination = Camera.main.ScreenToWorldPoint(emit);
            destination.Set(destination.x, destination.y, destination.z + 1f);

            var start = emit;
            transform.position = start;
            
            gameObject.SetActive(true);
            
            // 移動し始める
            await transform.DOMove(transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)), AppearTime).SetEase(Ease.OutCubic).AsyncWaitForCompletion();
            
            await transform.DOMove(destination, AppearTime).SetEase(Ease.InCubic).AsyncWaitForCompletion();

            gameObject.SetActive(false);
            Release();
        }
    }
}