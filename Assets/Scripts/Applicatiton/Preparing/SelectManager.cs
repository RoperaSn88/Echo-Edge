using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;
using Unity.VisualScripting;

using EchoEdge.App.Scenario;
using EchoEdge.App.Scene;
using EchoEdge.Domain.Preparing;
using EchoEdge.Domain.Scenario;
using EchoEdge.Domain.Scene;
using EchoEdge.Infra.Audio;
using EchoEdge.Infra.Scenario;
using EchoEdge.Presenter.Preparing;

namespace EchoEdge.App.Preparing
{
    public class SelectManager : MonoBehaviour
    {
        public static SelectManager Instance;

        private const float FadeTime = 2.0f;

        /// <summary>
        /// タイトルロゴ表示後、選択肢を画面右側から出現させる際の移動時間
        /// </summary>
        private const float SelectableGroupSlideInDuration = 0.5f;

        /// <summary>
        /// 選択肢を待機させておく、既定位置からの X 方向のオフセット（画面外の右側）
        /// </summary>
        private const float SelectableGroupSlideInOffsetX = 800f;

        [SerializeField]
        private Image _panel;

        [SerializeField]
        private RectTransform _topRectTransform;

        private Stack<RectTransform> _placingStack = new Stack<RectTransform>();
        private Stack<Vector3> _placingStackOriginPos = new Stack<Vector3>();

        [SerializeField] private Transform _defaultPosition;
        [SerializeField] private SelectableGroup _selectableGroup;

        [SerializeField]
        [Tooltip("起動時に表示するタイトルロゴ。操作されるまで選択肢を出現させない")]
        private TitleLogoPresenter _titleLogoPresenter;

        [SerializeField]
        [Tooltip("ステージごとのシナリオ再生要否設定。StartText がステージ選択確定時に参照する")]
        private StageScenarioPlaybackSettings _stageScenarioPlaybackSettings;

        public Vector3 DefaultLocalPosition => _defaultPosition.localPosition;

        /// <summary>
        /// ステージごとのシナリオ再生要否設定。
        /// StartText がステージ選択確定時に、選択中ステージのシナリオを再生するか判定するために参照する。
        /// </summary>
        public StageScenarioPlaybackSettings StageScenarioPlaybackSettings => _stageScenarioPlaybackSettings;

        /// <summary>
        /// スタックの最上位にあるRectTransform
        /// </summary>
        public RectTransform TopItem => _placingStack.Count > 0 ? _placingStack.Peek() : null;

        /// <summary>
        /// 最初の選択肢グループの RectTransform
        /// </summary>
        private RectTransform _selectableGroupRectTransform;

        /// <summary>
        /// 最初の選択肢グループの本来の位置
        /// </summary>
        private Vector2 _selectableGroupDefaultAnchoredPosition;

        /// <summary>
        /// 指定されたRectTransformがトップ配置スタックに含まれているか確認する
        /// </summary>
        public bool IsPlacedAtTop(RectTransform rect) => _placingStack.Contains(rect);

        /// <summary>
        /// 初期化時
        /// </summary>
        void Awake()
        {
            // 選択肢はタイトルロゴが操作された後に画面右側から出現させるため、
            // あらかじめ画面外の右側へ退避させておく
            _selectableGroupRectTransform = _selectableGroup.GetComponent<RectTransform>();
            _selectableGroupDefaultAnchoredPosition = _selectableGroupRectTransform.anchoredPosition;

            var hiddenAnchoredPosition = _selectableGroupDefaultAnchoredPosition;
            hiddenAnchoredPosition.x += SelectableGroupSlideInOffsetX;
            _selectableGroupRectTransform.anchoredPosition = hiddenAnchoredPosition;
        }

        /// <summary>
        /// 起動時
        /// </summary>
        async void Start()
        {
            Instance = this;

            if (AudioManager.Instance == null)
            {
                throw new InvalidOperationException("AudioManager.Instance が見つかりません。シーンに AudioManager を配置してください。");
            }

            // 初回起動時はプロローグシナリオを再生してから Preparing シーンを起動する。
            // 再生済みの場合は従来どおり Preparing シーンを直接起動する。
            if (!PrologueSaveManager.HasPlayedPrologue())
            {
                await PlayPrologueThenLoadPreparingAsync();
            }

            await AudioManager.Instance.PlayBgm(BgmAudioType.Title, true);

            _panel.gameObject.SetActive(true);
            var c = _panel.color;
            c.a = 1f;
            _panel.color = c;

            await _panel.DOFade(0f,FadeTime);
            _panel.gameObject.SetActive(false);

            // タイトルロゴを表示し、何らかの操作を受け付けてから選択肢を出現させる
            if (_titleLogoPresenter != null)
            {
                await _titleLogoPresenter.PresentAsync();
            }
            else
            {
                Debug.LogWarning("TitleLogoPresenter が設定されていません。タイトルロゴの表示をスキップします。");
            }

            await ShowFirstSelectableGroup();

            Selecting().Forget();
        }

        /// <summary>
        /// 退避させておいた最初の選択肢グループを、画面右側から本来の位置へ出現させる
        /// </summary>
        private async UniTask ShowFirstSelectableGroup()
        {
            _selectableGroupRectTransform.gameObject.SetActive(true);
            await _selectableGroupRectTransform
                .DOAnchorPos(_selectableGroupDefaultAnchoredPosition, SelectableGroupSlideInDuration)
                .SetEase(Ease.OutQuad)
                .ToUniTask();
        }

        /// <summary>
        /// 選択の管理をする
        /// </summary>
        /// <returns></returns>
        async UniTask Selecting()
        {
            ISelectableManager manager = _selectableGroup;
            while (manager != null)
            {
                manager = await manager.Selecting();
            }
        }

        public async UniTask PlaceAtTop(RectTransform rectTransform)
        {
            //対象の元の位置を保存しておく
            _placingStackOriginPos.Push(rectTransform.localPosition);

            // すでにスタックしてるやつらは右にズラす
            if (_placingStack.Count != 0)
            {
                foreach (var rect in _placingStack)
                {
                    rect.DOLocalMoveX(rect.localPosition.x + 600f, 0.5f).SetEase(Ease.InQuad);
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            // 新しいやつをスタックしてるやつの上に置く
            await rectTransform.DOLocalMoveY(_topRectTransform.localPosition.y, 0.5f).SetEase(Ease.OutQuad);
            _placingStack.Push(rectTransform);
        }

        public async UniTask RemoveFromTop()
        {
            if (_placingStack.Count == 0) return;

            // 最上位にいたやつを元の位置に戻す
            var targetRect = _placingStack.Pop();
            var originPos = _placingStackOriginPos.Pop();
            targetRect.DOLocalMove(originPos, 0.5f).SetEase(Ease.OutQuad).ToUniTask().Forget();

            // スタックしてるやつらを左にズラす
            foreach (var rect in _placingStack)
            {
                rect.DOLocalMoveX(rect.localPosition.x - 600f, 0.5f).SetEase(Ease.InQuad);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        }

        /// <summary>
        /// プロローグシナリオを再生し、再生済みとして保存したうえで Preparing シーンを起動する。
        /// 次回起動時には <see cref="PrologueSaveManager.HasPlayedPrologue"/> が true を返すため、
        /// このメソッドは呼び出されなくなる。
        /// プロローグの再生に失敗した場合でも、finally で Preparing シーンの起動だけは必ず行う
        /// （この場合は再生済みとして保存しないため、次回起動時に再度プロローグの再生を試みる）。
        /// </summary>
        private async UniTask PlayPrologueThenLoadPreparingAsync()
        {
            try
            {
                await ScenarioStageLoader.PlayPrologueScenarioAsync();
                PrologueSaveManager.SaveProloguePlayed();
            }
            catch (Exception e)
            {
                Debug.LogError($"プロローグシナリオの再生に失敗しました。Preparing シーンを起動します: {e}");
            }
            finally
            {
                SceneLoader.Unload(GameScene.Scenario);
            }
        }
    }
}
