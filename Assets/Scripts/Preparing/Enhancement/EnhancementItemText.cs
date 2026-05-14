using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// 強化画面の各強化項目の基底クラス。
/// 強化実行後に Overseer の Magic アニメーションを再生する。
/// 具体的な強化処理はサブクラスで実装する。
/// </summary>
public abstract class EnhancementItemText : TMPSelectObject
{
    private const string MagicTrigger = "MagicT";

    /// <summary>
    /// Overseer の Animator（インスペクターから設定する）
    /// </summary>
    [SerializeField]
    protected Animator _overseerAnimator;

    /// <summary>
    /// 強化成功 / 石不足を一時的に表示するテキスト
    /// </summary>
    [SerializeField]
    protected TextMeshProUGUI _feedbackText;

    /// <inheritdoc/>
    public override async UniTask OnDecide()
    {
        bool success = TryEnhance();

        if (_feedbackText != null)
        {
            _feedbackText.text = success ? "強化成功！" : "石が足りません";
            _feedbackText.gameObject.SetActive(true);
        }

        if (success)
        {
            if (_overseerAnimator != null)
            {
                _overseerAnimator.SetTrigger(MagicTrigger);
            }
            else
            {
                Debug.LogWarning($"{GetType().Name}: Overseer の Animator が設定されていません。");
            }

            OnEnhanceSuccess();
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(0.8f));

        if (_feedbackText != null)
        {
            _feedbackText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 強化を試みる。石が足りなければ false を返す。
    /// </summary>
    /// <returns>強化に成功したか</returns>
    protected abstract bool TryEnhance();

    /// <summary>
    /// 強化成功時に呼ばれる追加処理（UI更新等）。
    /// </summary>
    protected virtual void OnEnhanceSuccess()
    {
        EnhancementStatusText.Instance?.RefreshText();
    }
}
