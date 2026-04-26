using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

/// <summary>
/// プレイヤーの残像を表すオブジェクト。オブジェクトプールで管理される。
/// </summary>
public class AfterimageObject : ObjectPooler
{
    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    /// <summary>
    /// 出現時の初期アルファ値（少し透過した状態）
    /// </summary>
    private const float InitialAlpha = 0.7f;

    /// <summary>
    /// 透明になるまでのトゥイーン時間
    /// </summary>
    private const float FadeTime = 0.3f;

    /// <summary>
    /// プールから取り出した際の初期化処理。
    /// 実際の出現はAfterimageAppearで行う。
    /// </summary>
    public override async UniTask Appear()
    {
        gameObject.SetActive(false);
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// 残像を出現させ、フェードアウト後にプールへ返却する。
    /// </summary>
    /// <param name="sprite">プレイヤーの現在のスプライト</param>
    /// <param name="position">出現位置</param>
    /// <param name="rotation">出現時の回転</param>
    public async UniTaskVoid AfterimageAppear(Sprite sprite, Vector3 position, Quaternion rotation)
    {
        // プレイヤーと同じスプライトを設定する
        _spriteRenderer.sprite = sprite;
        transform.position = position;
        transform.rotation = rotation;

        // 少し透過した状態に設定する
        var color = _spriteRenderer.color;
        color.a = InitialAlpha;
        _spriteRenderer.color = color;

        gameObject.SetActive(true);

        // アルファ値を0（完全に透明）にするトゥイーンを短時間で実行する
        await _spriteRenderer.DOFade(0f, FadeTime).ToUniTask();

        // トゥイーン終了後、オブジェクトを非表示にしプールへ返す
        Release();
    }
}
