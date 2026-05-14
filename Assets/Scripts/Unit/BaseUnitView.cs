using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class BaseUnitView: MonoBehaviour, IDamageActivator, IUnitView, IDisposable
{
    private const string EnemyAnimPath = "Assets/Addressables/Animator/";
    
    private int height;

    private int width;
    
    [SerializeField]
    private Animator _animator;

    public Animator Animator => _animator;

    [SerializeField]
    private SpriteRenderer _renderer;

    /// <summary>
    ///  移動に使用するベクトル
    /// </summary>
    private Vector3 _moveVec;
    
    /// <summary>
    /// アニメ中に攻撃を行うフラグ
    /// </summary>
    private bool _animationFlag;

    public bool AnimationFlag => _animationFlag;
    
    /// <summary>
    /// 死んだか
    /// </summary>
    private bool _isDeath;

    private const float MoveTime = 0.15f;
    private const float DeadFadeTime = 0.5f;

    public async UniTask SetAnimator(EnemyKinds enemyID)
    {
        var data = await Addressables.LoadAssetAsync<RuntimeAnimatorController>(EnemyAnimPath + enemyID + ".controller").ToUniTask();
        if (data != null)
        {
            _animator.runtimeAnimatorController = data;
        }
        else
        {
            Debug.LogWarning($"EnemyAnimPath {EnemyAnimPath + enemyID + ".controller"} のアニメーターを読み込めませんでした。");
        }
    }

    /// <summary>
    /// 表示位置を初期化する。UnitSpawner から呼び出す。
    /// </summary>
    /// <param name="h">配置する縦座標</param>
    /// <param name="w">配置する横座標</param>
    public virtual async UniTask Setup(int h, int w, EnemyKinds enemyID)
    {
        height = h;
        width = w;
        _isDeath = false;
        _animationFlag = false;
        if (_renderer != null)
        {
            var color = _renderer.color;
            color.a = 1f;
            _renderer.color = color;
        }
        transform.localPosition = new Vector3(w, 0, h);
        await SetAnimator(enemyID);
        gameObject.SetActive(true);
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
    
    /// <summary>
    /// 攻撃アニメーションを実行する前のカメラの移動を行う
    /// </summary>
    public async UniTask WaitToCameraZoom()
    {
        await CameraManager.Instance.ActSetCameraTarget(transform.position);
    }
    
    /// <summary>
    /// 攻撃のアニメーションを開始
    /// </summary>
    public async UniTask WaitAttackAnim()
    {
        _animator.SetTrigger("AttackT");
        _animationFlag = false;
        
        await UniTask.WaitUntil(() => _animationFlag);
    }
    
    /// <summary>
    /// 攻撃のアニメーションを開始
    /// </summary>
    public async UniTask WaitSpecificAnim()
    {
        _animator.SetTrigger("SkillT");
        _animationFlag = false;
        
        await UniTask.WaitUntil(() => _animationFlag);
    }

    public void ActiveAttack()
    {
        _animationFlag = true;
    }

    public async UniTask Attack()
    {
        
    }

    public async UniTask Damage()
    {
        Time.timeScale = 0.001f;
        CameraManager.Instance.ActSetCameraTarget(transform.position).Forget();

        var targetUnit = MapManager.Instance.GetUnitAt(height, width);
        if (targetUnit == null)
        {
            Time.timeScale = 1.0f;
            return;
        }

        var targetStatus = targetUnit.GetStatus();
        BattleManager.RegisterEnemy(targetStatus);
        var damageValue = await BattleManager.EnemyDamage();
        Debug.Log("value: " + damageValue.isDeath);
        
        UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", transform.position).Forget();

        if (damageValue.isDeath)
        {
            _animator.SetTrigger("DeadT");
            UIPresenter.Instance.AppearEnergy(transform.position, targetStatus.Energy).Forget();
            await UniTask.Delay(TimeSpan.FromSeconds(0.7f), ignoreTimeScale:true);
        }
        else
        {
            UIPresenter.Instance.AppearEnergy(transform.position, targetStatus.Energy / 2).Forget();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), ignoreTimeScale:true);
        }
        
        Time.timeScale = 1.0f;

        // CameraManager.Instance.ActResetCameraTarget().Forget();

        if (damageValue.isDeath)
        {
            MapManager.Instance.RemoveUnitAt(height, width);
            if (targetUnit is IEnemyUnit)
            {
                GameClearManager.OnEnemyDead(height, width, targetStatus.Experience);
            }
            //Destroyするが、後でオブジェクトプールにする
            Dispose();
            if (UnitSpawner.Instance != null)
            {
                UnitSpawner.Instance.ReturnView(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void Dead()
    {
        _isDeath = true;
        _renderer.DOFade(0f, DeadFadeTime);
    }

    public void Dispose()
    {
    }
}
