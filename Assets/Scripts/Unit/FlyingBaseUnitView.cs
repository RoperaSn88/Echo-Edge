using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 飛行能力を持つユニット用の View。
/// BaseUnitView を継承し、IFlyingUnitView を実装することで飛び上がり・ビーム攻撃アニメーションを提供する。
/// </summary>
public class FlyingBaseUnitView : BaseUnitView, IFlyingUnitView
{
    /// <summary>
    /// 地上のY座標（飛行前の元の高さ）
    /// </summary>
    private float _groundY;

    private const float FlyHeight = 3f;
    private const float FlyTime = 0.5f;

    /// <inheritdoc/>
    public override async UniTask Setup(int h, int w, EnemyKinds enemyID)
    {
        await base.Setup(h, w, enemyID);
        _groundY = transform.localPosition.y;
    }

    /// <inheritdoc/>
    public async UniTask WaitFlyAnim()
    {
        await transform.DOLocalMoveY(_groundY + FlyHeight, FlyTime).SetEase(Ease.OutQuad);
    }

    /// <inheritdoc/>
    public async UniTask WaitBeamAnim()
    {
        // ビームのアニメーショントリガー（アニメーター実装後に対応）
        // _animator.SetTrigger("BeamT");
        await UniTask.Delay(TimeSpan.FromSeconds(1f));
        await transform.DOLocalMoveY(_groundY, FlyTime).SetEase(Ease.InQuad);
    }
}
