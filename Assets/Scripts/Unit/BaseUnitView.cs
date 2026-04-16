using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BaseUnitView: MonoBehaviour, IDamageActivator, IDisposable
{
    private int height;

    private int width;
    
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private Image _image;

    /// <summary>
    ///  移動に使用するベクトル
    /// </summary>
    private Vector3 _moveVec;
    
    /// <summary>
    /// アニメ中に攻撃を行うフラグ
    /// </summary>
    private bool _attackFlug;
    
    /// <summary>
    /// 死んだか
    /// </summary>
    private bool _isDeath;

    private const float MoveTime = 0.15f;
    private const float DeadFadeTime = 0.5f;

    /// <summary>
    /// 表示位置を初期化する。UnitSpawner から呼び出す。
    /// </summary>
    /// <param name="h">配置する縦座標</param>
    /// <param name="w">配置する横座標</param>
    public void Setup(int h, int w)
    {
        height = h;
        width = w;
        transform.localPosition = new Vector3(w, 0, h);
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
        Vector3 targetPos = new Vector3(PlayerController.Instance.transform.position.x + transform.position.x, 0, PlayerController.Instance.transform.position.z + transform.position.z) / 2;
        await CameraManager.Instance.ActSetCameraTarget(targetPos);
        
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
        CameraManager.Instance.ActSetCameraTarget(transform.position).Forget();
        
        BattleManager.RegisterEnemy(MapManager.Instance.GetUnitAt(height, width).GetStatus());
        var damageValue = await BattleManager.EnemyDamage();
        
        Time.timeScale = 1.0f;
        
        UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", transform.position).Forget();

        if (damageValue.isDeath)
        {
            _animator.SetTrigger("DeadT");
            UIPresenter.Instance.AppearEnergy(transform.position, MapManager.Instance.GetUnitAt(height, width).GetStatus().Energy);
            await UniTask.WaitUntil(() => _isDeath);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        }
        else
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1.0f));
        }

        CameraManager.Instance.ActResetCameraTarget().Forget();

        if (damageValue.isDeath)
        {
            //Destroyするが、後でオブジェクトプールにする
            Dispose();
            // Destroy(gameObject);
            gameObject.SetActive(false);
        }
    }

    public void Dead()
    {
        _isDeath = true;
        _image.DOFade(0f, DeadFadeTime);
    }

    public void Dispose()
    {
    }
}