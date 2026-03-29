using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class BaseUnitView: MonoBehaviour, IDamageActivator
{
    [SerializeField, Tooltip("表示のためのクラス(いじるな)")]
    private BaseUnit _baseUnit;

    [SerializeField]
    private int height;

    [SerializeField]
    private int width;
    
    /// <summary>
    /// 仮のステータス
    /// </summary>
    [SerializeField]
    private BattleStatus _status;
    
    [SerializeField]
    private Animator _animator;

    /// <summary>
    ///  移動に使用するベクトル
    /// </summary>
    private Vector3 _moveVec;
    
    /// <summary>
    /// アニメ中に攻撃を行うフラグ
    /// </summary>
    private bool _attackFlug;

    private const float MoveTime = 0.15f;

    void Start()
    {
        // 登録用
        // 後ほどcsvで読み取る方法に変更する
        _baseUnit = new BaseUnit(this, height, width);
        _baseUnit.RegistarStatus(_status);
    }

    /// <summary>
    /// 左に移動するのでマイナス
    /// </summary>
    /// <param name="y">縦方向の移動量</param>
    /// <param name="x">横方向の移動量</param>
    public async UniTask Move(int y, int x)
    {
        // 横方向はマイナス方向に進めるため、負の値にする
        _moveVec = new Vector3(x, 0, y);
        
        // 移動をする
        await transform.DOLocalMove(_moveVec, MoveTime).SetEase(Ease.OutQuad);
        await UniTask.Delay(TimeSpan.FromSeconds(MoveTime * 3f));
        
        // 位置を更新する
        height = y;
        width = x;
    }

    public async UniTask WaitAttack()
    {
        await CameraManager.Instance.ActSetCameraTarget(transform);
        
        _animator.SetTrigger("AttackT");
        _attackFlug = false;
        
        await UniTask.WaitUntil(() => _attackFlug);
    }

    public void ActiveAttack()
    {
        _attackFlug = true;
    }

    public async UniTask Attack()
    {
        
    }

    public async UniTask Damage()
    {
        Time.timeScale = 0.001f;
        CameraManager.Instance.ActSetCameraTarget(transform).Forget();
        
        BattleManager.RegisterEnemy(MapManager.Instance.GetUnitAt(height, width).GetStatus());
        var damageValue = await BattleManager.EnemyDamage();
        
        Time.timeScale = 1.0f;

        UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", transform).Forget();

        if (damageValue.isDeath)
        {
            Debug.Log("敵を消すよ");
        }
        
        await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

        CameraManager.Instance.ActResetCameraTarget().Forget();
    }
}