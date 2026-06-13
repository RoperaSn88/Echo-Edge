using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using CommonUI.Tutorial.Models;
using CommonUI.Tutorial.Views;
using Cysharp.Threading.Tasks;

namespace CommonUI.Tutorial
{
    /// <summary>
    /// テキストボックスのPresenter
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TextBoxPresenter : MonoBehaviour
    {
        [SerializeField, Tooltip("反映させるテキストボックス内のTMP")]
        private TextMeshProUGUI _textMeshPro;

        [SerializeField, Tooltip("テキストボックスのRectTransform")]
        private RectTransform _textBoxRectTransform;

        [SerializeField, Tooltip("マスクのRectTransform")]
        private CoachMarkView _coachMaskView;

        [SerializeField, Tooltip("矩形のアニメーションフレーム")]
        private RectangleFrame _rectangleFrame;

        [SerializeField, Tooltip("円のアニメーションフレーム")]
        private CircleFrame _circleFrame;

        [SerializeField, Tooltip("表示にかかる1文字あたりの秒.")]
        private float _textAnimDurationSec = 0.1f;

        [SerializeField, Tooltip("マスクカットアウト")]
        private MaskCutout _maskCutout;

        [SerializeField, Tooltip("指アイコン")]
        private FingerView _fingerView;

        [SerializeField, Tooltip("ピンを表示するかどうか.")]
        private bool _isPinEnabled = true;

        [SerializeField, Tooltip("ピンのViewクラス")]
        private ForwardingIcon _forwardingIcon;

        [SerializeField, Tooltip("ページドットのPresenter")]
        private PageDotPresenter _pageDotPresenter;

        [SerializeField, Tooltip("円環状エフェクト")]
        private RingEffect _ringEffect;

        /// <summary>
        /// 現在表示中のテキストのモデル.
        /// </summary>
        private TextBoxModel _model;

        private CancellationTokenSource _cts = new();

        private bool _isTextAnimating;

        /// <summary>
        /// UIの位置を取得しておくフィールド
        /// </summary>
        private readonly Vector3[] _corners = new Vector3[4];

        /// <summary>
        /// モデルのページを表示する.
        /// </summary>
        /// <param name="modelData">テキストボックスのデータ.</param>
        /// <param name="cancellationToken">キャンセレーショントークン.</param>
        public async Awaitable ShowModelAsync(TextBoxModel modelData, CancellationToken cancellationToken)
        {
            _model = modelData;
            var maxPageCount = _model.Models.Count;

            // テキストが１ページもない場合は処理を終了する.
            if (maxPageCount == 0)
            {
                throw new InvalidOperationException("テキストが1ページもありません");
            }

            var pageIndex = 0;
            _pageDotPresenter.Initialize(maxPageCount);
            SetBasePosition(_model.Position);
            AdjustPosition(_model);
            bool isSkip = false;

            try
            {
                while (pageIndex < maxPageCount)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _ = ShowPageAsync(pageIndex);

                    PageTurn turn;
                    // 進むか戻るの入力があるまで待ち、前にページがないときは再度入力を待つ.
                    await UniTask.WaitUntil(() => !TextBasePresenter.MouseClick.Mouse.MouseClick.IsPressed());
                    do
                    {
                        turn = await WaitUntilInputAsync(cancellationToken);
                    } while (turn == PageTurn.Prev && pageIndex == 0);
                    
                    Debug.Log(turn);
                    switch (turn)
                    {
                        case PageTurn.Next:
                            pageIndex++;
                            _pageDotPresenter.Next();
                            break;
                        case PageTurn.Prev when pageIndex > 0:
                            pageIndex--;
                            _pageDotPresenter.Prev();
                            break;
                        default:
                            isSkip = true;
                            break;
                    }

                    if (isSkip)
                    {
                        break;
                    }
                }
                isSkip = false;
            }
            finally
            {
                
            }
            
        }

        /// <summary>
        /// 指定されたインデックスのページを表示するメソッド.
        /// </summary>
        /// <param name="pageIndex">表示するページ</param>
        private async Awaitable ShowPageAsync(int pageIndex)
        {
            try
            {
                var currentPage = _model.Models[pageIndex];
                var text = currentPage.Text;
                if (text == null)
                {
                    throw new InvalidOperationException("テキストが設定されていません. ページ数: " + (pageIndex + 1));
                }

                SetCoachMark(currentPage.CoachMark);
                SetFingerIcon(currentPage);
                SetRingEffect(currentPage);

                _isTextAnimating = true;

                try
                {
                    ResetToken();
                    await _textMeshPro.AnimateTextAsync(text, TimeSpan.FromSeconds(_textAnimDurationSec), _cts.Token);
                }
                finally
                {
                    _isTextAnimating = false;
                    if (_isPinEnabled)
                    {
                        ResetToken();
                        await _forwardingIcon.PlayAnimAsync(_cts.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                // キャンセルは正常系（ユーザーの早送り等）なのでログしたくない。
                if (ex is OperationCanceledException)
                {
                    return;
                }
                throw;
            }
        }

        /// <summary>
        /// 進む戻るの入力を受け取るまで待つ.
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns><see cref="PageTurn"/>を返す.</returns>
        private async Awaitable<PageTurn> WaitUntilInputAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Awaitable.NextFrameAsync(cancellationToken);

                while (true)
                {
                    if (TextBasePresenter.MouseClick.Mouse.MouseClick.IsPressed())
                    {
                        // 文字送り中にクリックされた場合は全文表示
                        if (_isTextAnimating)
                        {
                            _cts.Cancel();
                            await UniTask.WaitUntil(() => !TextBasePresenter.MouseClick.Mouse.MouseClick.IsPressed());
                            continue;
                        }

                        return PageTurn.Next;
                    }

                    // // 戻るボタンで前のページ表示
                    // if (Input.GetKeyDown(KeyCode.LeftArrow))
                    // {
                    //     return PageTurn.Prev;
                    // }

                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }
            catch
            {

            }
            return PageTurn.Invalid;
        }

        /// <summary>
        /// キャンセレーショントークンをリセットする.
        /// </summary>
        private void ResetToken()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// テキストボックスの位置を決める
        /// </summary>
        /// <param name="textPosition">テキストボックスをどこに配置するか指定したもの</param>
        private void SetBasePosition(TextPositions textPosition)
        {
            // CoachMarkがズレてしまうため、anchoredPositionを初期化
            _textBoxRectTransform.anchoredPosition = Vector3.zero;

            // anchorとpivotを調節して,指定した位置に配置させる
            switch (textPosition)
            {
                case TextPositions.TopLeft:
                    _textBoxRectTransform.anchorMax = TutorialConstants.TextBoxPositions.TopLeftVector;
                    _textBoxRectTransform.anchorMin = TutorialConstants.TextBoxPositions.TopLeftVector;
                    _textBoxRectTransform.pivot = TutorialConstants.TextBoxPositions.TopLeftVector;
                    break;
                case TextPositions.TopCenter:
                    _textBoxRectTransform.anchorMax = TutorialConstants.TextBoxPositions.TopCenterVector;
                    _textBoxRectTransform.anchorMin = TutorialConstants.TextBoxPositions.TopCenterVector;
                    _textBoxRectTransform.pivot = TutorialConstants.TextBoxPositions.TopCenterVector;
                    break;
                case TextPositions.TopRight:
                    _textBoxRectTransform.anchorMax = TutorialConstants.TextBoxPositions.TopRightVector;
                    _textBoxRectTransform.anchorMin = TutorialConstants.TextBoxPositions.TopRightVector;
                    _textBoxRectTransform.pivot = TutorialConstants.TextBoxPositions.TopRightVector;
                    break;
                case TextPositions.MiddleLeft:
                    _textBoxRectTransform.anchorMax = TutorialConstants.TextBoxPositions.MiddleLeftVector;
                    _textBoxRectTransform.anchorMin = TutorialConstants.TextBoxPositions.MiddleLeftVector;
                    _textBoxRectTransform.pivot = TutorialConstants.TextBoxPositions.MiddleLeftVector;
                    break;
                case TextPositions.MiddleCenter:
                    _textBoxRectTransform.anchorMax = TutorialConstants.TextBoxPositions.MiddleCenterVector;
                    _textBoxRectTransform.anchorMin = TutorialConstants.TextBoxPositions.MiddleCenterVector;
                    _textBoxRectTransform.pivot = TutorialConstants.TextBoxPositions.MiddleCenterVector;
                    break;
                case TextPositions.MiddleRight:
                    _textBoxRectTransform.anchorMax = TutorialConstants.TextBoxPositions.MiddleRightVector;
                    _textBoxRectTransform.anchorMin = TutorialConstants.TextBoxPositions.MiddleRightVector;
                    _textBoxRectTransform.pivot = TutorialConstants.TextBoxPositions.MiddleRightVector;
                    break;
                case TextPositions.BottomLeft:
                    _textBoxRectTransform.anchorMax = TutorialConstants.TextBoxPositions.BottomLeftVector;
                    _textBoxRectTransform.anchorMin = TutorialConstants.TextBoxPositions.BottomLeftVector;
                    _textBoxRectTransform.pivot = TutorialConstants.TextBoxPositions.BottomLeftVector;
                    break;
                case TextPositions.BottomCenter:
                    _textBoxRectTransform.anchorMax = TutorialConstants.TextBoxPositions.BottomCenterVector;
                    _textBoxRectTransform.anchorMin = TutorialConstants.TextBoxPositions.BottomCenterVector;
                    _textBoxRectTransform.pivot = TutorialConstants.TextBoxPositions.BottomCenterVector;
                    break;
                case TextPositions.BottomRight:
                    _textBoxRectTransform.anchorMax = TutorialConstants.TextBoxPositions.BottomRightVector;
                    _textBoxRectTransform.anchorMin = TutorialConstants.TextBoxPositions.BottomRightVector;
                    _textBoxRectTransform.pivot = TutorialConstants.TextBoxPositions.BottomRightVector;
                    break;
            }
        }

        /// <summary>
        /// コーチマークの位置・サイズを設定し、アニメーションフレームを調整する
        /// </summary>
        /// <param name="model">元となるモデル</param>
        private void SetCoachMark(CoachMarkModel model)
        {
            // 指定された名前のオブジェクトを探す
            var targetObject = GameObject.Find(model.TargetObjectName);

            // オブジェクトが見つからなかった場合はエラーログを出力して終了
            if (targetObject == null)
            {
                throw new NullReferenceException("対象のオブジェクトが見つかりませんでした. 指定したオブジェクト名: " + model.TargetObjectName);
            }

            // マスクの位置をキャッシュしておく
            var anchoredPosition = _coachMaskView.MaskRectTransform.anchoredPosition;

            // 対象のゲームオブジェクトにRectTransformがある場合
            if (targetObject.transform is RectTransform targetRect)
            {
                // 対象のゲームオブジェクトの4端のワールド座標を取得し、中心を計算。その結果を座標に当てはめる。
                targetRect.GetWorldCorners(_corners);
                var targetPosition = (_corners[0] + _corners[2]) / 2;

                _coachMaskView.MaskRectTransform.position = targetPosition;

                // 元のコーチマークの位置のままなので再キャッシュ
                anchoredPosition = _coachMaskView.MaskRectTransform.anchoredPosition;

                // グラデーションの場合、処理をして終了
                if (model.IsGradiate)
                {
                    switch (model.Shape)
                    {
                        case ShapeKinds.Rectangle:
                            var targetWidth = targetRect.rect.width + model.Radius;
                            var targetHeight = targetRect.rect.height + model.Radius;
                            _maskCutout.SetRectangle(
                                anchoredPosition.x,
                                anchoredPosition.y,
                                targetWidth,
                                targetHeight,
                                model.PaddingRadius,
                                model.GradiateRadius);
                            SetAnimationFrame(ShapeKinds.Rectangle);
                            return;

                        case ShapeKinds.Circle:
                            _maskCutout.SetCircle(
                                anchoredPosition.x,
                                anchoredPosition.y,
                                model.Radius,
                                model.PaddingRadius,
                                model.GradiateRadius);
                            SetAnimationFrame(ShapeKinds.Circle);
                            return;
                    }
                }

                // 通常コーチマークの処理
                // コーチマークの形をモデルに合わせて変更する
                _coachMaskView.SetSprite(model.Shape);

                // モデルの形によってマスクの形を変更する
                switch (model.Shape)
                {
                    // 矩形の場合はその形のサイズに合わせる。対象のサイズにモデルの半径を加えたサイズを計算。
                    case ShapeKinds.Rectangle:
                        var targetWidth = targetRect.rect.width + model.Radius;
                        var targetHeight = targetRect.rect.height + model.Radius;
                        _coachMaskView.MaskRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                        _coachMaskView.MaskRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

                        // アニメーションフレームを設定する
                        SetAnimationFrame(model.Shape);
                        return;

                    // 円形の場合はモデルの半径の大きさに合わせる
                    case ShapeKinds.Circle:
                        _coachMaskView.MaskRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                            model.Radius);
                        _coachMaskView.MaskRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                            model.Radius);

                        // アニメーションフレームを設定する
                        SetAnimationFrame(model.Shape);
                        return;
                }
            }

            // RectTransformがない場合
            // オブジェクトの場所からUIの位置を計算する。計算した結果にUIを位置させる。
            var screenPosition = Camera.main.WorldToScreenPoint(targetObject.transform.position);
            _coachMaskView.MaskRectTransform.position = screenPosition;

            // 元のコーチマークの位置のままなので再キャッシュ
            anchoredPosition = _coachMaskView.MaskRectTransform.anchoredPosition;

            // RectTransformがない場合、モデルに関係なく円形で対応する
            // グラデ有りならば、グラデーション円形で対応する
            if (model.IsGradiate)
            {
                _maskCutout.SetCircle(
                    anchoredPosition.x,
                    anchoredPosition.y,
                    model.Radius,
                    model.PaddingRadius,
                    model.GradiateRadius);
            }
            else
            {
                // 円形で対応する。
                _coachMaskView.SetSprite(ShapeKinds.Circle);
                _coachMaskView.MaskRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, model.Radius);
                _coachMaskView.MaskRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, model.Radius);
            }

            // アニメーションフレームを設定する
            SetAnimationFrame(ShapeKinds.Circle);
        }

        /// <summary>
        /// コーチマークにアニメーションフレームを合わせる
        /// </summary>
        /// <param name="shape">フレームの形</param>
        private void SetAnimationFrame(ShapeKinds shape)
        {
            // アニメーションフレームが設定されていない場合はエラーを出す
            if (_rectangleFrame == null)
            {
                throw new NullReferenceException("矩形アニメーションフレームが設定されていません。");
            }

            if (_circleFrame == null)
            {
                throw new NullReferenceException("円形アニメーションフレームが設定されていません。");
            }

            switch (shape)
            {
                // 矩形の場合
                case ShapeKinds.Rectangle:
                    _rectangleFrame.gameObject.SetActive(true);
                    _circleFrame.gameObject.SetActive(false);
                    _rectangleFrame.SetPosition(_coachMaskView.MaskRectTransform);
                    _rectangleFrame.SetSize(_coachMaskView.MaskRectTransform);
                    _rectangleFrame.SetAnimation(_coachMaskView.MaskRectTransform);
                    break;

                // 円形の場合
                case ShapeKinds.Circle:
                    _rectangleFrame.gameObject.SetActive(false);
                    _circleFrame.gameObject.SetActive(true);
                    _circleFrame.SetPosition(_coachMaskView.MaskRectTransform);
                    _circleFrame.SetSize(_coachMaskView.MaskRectTransform);
                    _circleFrame.SetAnimation(_coachMaskView.MaskRectTransform);
                    break;
            }
        }

        /// <summary>
        /// コーチマークから相対座標を設定する
        /// </summary>
        /// <param name="model">テキストボックスモデル</param>
        private void AdjustPosition(TextBoxModel model)
        {
            // 両方ともZeroならば相対座標の設定は行わない。
            if (model.RadiusHorizontalOffset == ValueKinds.Zero &&
                model.RadiusVerticalOffset == ValueKinds.Zero)
            {
                return;
            }

            // コーチマークの座標を取得し、中心座標を計算する。
            _coachMaskView.MaskRectTransform.GetWorldCorners(_corners);
            var targetPosition = (_corners[0] + _corners[2]) / 2;

            // テキストボックスの座標をコーチマークの中心に合わせる。
            _textBoxRectTransform.position = targetPosition;

            // コーチマークとテキストボックスの中心を合わせるため、テキストボックスの4端のワールド座標を取得し、中心を計算する。
            _textBoxRectTransform.GetWorldCorners(_corners);
            var textBoxPosition = (_corners[0] + _corners[2]) / 2;

            // コーチマークの中心とテキストボックスの中心の差分を計算する。
            var offset = targetPosition - textBoxPosition;

            // 差分と指定したズレの分ズラす。
            _textBoxRectTransform.position += offset + new Vector3((float)model.RadiusHorizontalOffset,
                (float)model.RadiusVerticalOffset, 0);
        }

        /// <summary>
        /// 指アイコンを設定する
        /// </summary>
        /// <param name="model">あるページのモデル</param>
        private void SetFingerIcon(TextModel model)
        {
            if (model.IsFingerIconEnabled)
            {
                _fingerView.gameObject.SetActive(true);
                _fingerView.SetIcon(_coachMaskView.MaskRectTransform, model.FingerIconDirection);
            }
            else
            {
                _fingerView.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// エフェクトを配置する
        /// </summary>
        private void SetRingEffect(TextModel model)
        {
            if(_ringEffect == null)
            {
                throw new NullReferenceException("円環状エフェクトが設定されていません。");
            }

            if (model.IsEffectEnabled)
            {
                _ringEffect.gameObject.SetActive(true);
                _ringEffect.SetPosition(_coachMaskView.MaskRectTransform);
                _ringEffect.SetSize(_coachMaskView.MaskRectTransform);
            }
            else
            {
                _ringEffect.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        /// <summary>
        /// 入力結果が進むか戻すかを表す列挙型.
        /// </summary>
        private enum PageTurn : byte
        {
            Next = 0,
            Prev = 1,
            Invalid = 255
        }
    }
}

