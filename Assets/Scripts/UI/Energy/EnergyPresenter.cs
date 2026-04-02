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
        public RectTransform _destinationRect;
        
        /// <summary>
        /// 出現する位置のTransform
        /// </summary>
        public Vector3 emit;

        [SerializeField]
        private Light _light;

        private const float AppearTime = 0.5f;
        
        private const float LightIntensity = 5f;
        
        public void SetPosition(RectTransform rect, Vector3 trans)
        {
            _destinationRect = rect;
            emit = trans;
        }

        public async override UniTask Appear()
        {
            // rectTransformのグローバルの位置をとってくる
            // これを終点とする

            // Screen Space - Overlay の場合:
            // _destinationRect.position はスクリーン座標なので、
            // z にカメラからの距離（奥行き）を入れて ScreenToWorldPoint で変換する
            var screenPos = _destinationRect.position;
            screenPos.Set(screenPos.x, screenPos.y, 1f);
            var destination = Camera.main.ScreenToWorldPoint(screenPos);
            Debug.Log(destination);

            var start = emit;
            transform.position = start;

            gameObject.SetActive(true);

            Time.timeScale = 0.4f;
            
            // 移動し始める
            transform.DOMove(
                transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(0, 1f), Random.Range(-1f, 1f)),
                AppearTime).SetEase(Ease.OutCubic).ToUniTask().Forget();


            await _light.DOIntensity(LightIntensity, AppearTime / 2).SetEase(Ease.OutCubic);
            await _light.DOIntensity(0, AppearTime / 2).SetEase(Ease.InQuad);
            
            
            // UIの位置に移動させるの、なんかダメなので無効
            // await UniTask.WhenAll(
            //     transform.DOMove(destination, AppearTime).SetEase(Ease.InCubic).ToUniTask(),
            //     _light.DOIntensity(0, AppearTime).SetEase(Ease.InCubic).ToUniTask()
            // );
            
            // エナジーの量をプレイヤーに増やす
            

            gameObject.SetActive(false);
            Release();
        }
    }
}