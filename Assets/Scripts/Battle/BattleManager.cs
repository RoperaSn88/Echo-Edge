using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UI.QTE;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    [SerializeField]
    private static BattleStatus _playerStatus;

    public static BattleStatus PlayerStatus => _playerStatus;

    [SerializeField]
    private static BattleStatus _enemyStatus;
    
    public static BattleStatus EnemyStatus => _enemyStatus;

    /// <summary>
    /// QTEを実行するためのフラグ
    /// 1回の攻撃につき一度だけ発動する。
    /// </summary>
    private static bool _QTEFlug = false;

    private static float _qteResult;

    /// <summary>
    /// 現在のコンボ数
    /// </summary>
    private static int _combo = 0;

    private const float PlayerDeathHitStopTimeScale = 0.05f;
    private const float PlayerDeathHitStopDurationSeconds = 0.2f;
    private const float PlayerDeathFadeDurationSeconds = 2.0f;

    private static bool _isPlayerDeathStarted;

    public static int Combo => _combo;

    public static void RegisterEnemy(BattleStatus targetStatus)
    {
        _enemyStatus = targetStatus;
    }

    public static void RegisterPlayer(BattleStatus targetStatus)
    {
        _playerStatus = targetStatus;
        _isPlayerDeathStarted = false;
    }

    public static void ResetQTE()
    {
        _QTEFlug = false;
    }

    /// <summary>
    /// コンボをリセットしてコンボテキストを非表示にする
    /// </summary>
    public static void ResetCombo()
    {
        _combo = 0;
        ComboPresenter.Instance?.ResetCombo();
    }

    public async static UniTask<(int damage, bool isDeath)> EnemyDamage()
    {
        if (!_QTEFlug)
        {
            _qteResult = await UIPresenter.Instance.AppearQTE(QTEKinds.Attack);
            _QTEFlug = true;
        }

        // コンボを増やしてテキストを更新する
        _combo++;
        float comboValue = 1.0f + (_combo - 1) * 0.1f;
        ComboPresenter.Instance?.SetCombo(_combo);

        return await _enemyStatus.Damage((int)(_playerStatus.Attack * (comboValue * comboValue) * _qteResult));
    }

    public async static UniTask<(int damage, bool isDeath)> PlayerDamage(float rate)
    {
        if (_isPlayerDeathStarted)
        {
            return (0, true);
        }

        if (!_QTEFlug)
        {
            _qteResult = await UIPresenter.Instance.AppearQTE(QTEKinds.Defend);
            _QTEFlug = true;
        } 

        var result = await _playerStatus.Damage((int)(_enemyStatus.Attack * rate * _qteResult));
        
        PlayerStatusPresenter.Instance.SetPlayerHP(_playerStatus.HP, _playerStatus.MaxHP);

        if (result.isDeath)
        {
            await StartPlayerDeathSequenceAsync();
        }
        
        return result;
    }

    private static async UniTask StartPlayerDeathSequenceAsync()
    {
        if (_isPlayerDeathStarted)
        {
            return;
        }

        _isPlayerDeathStarted = true;
        ResetQTE();
        ResetCombo();

        Time.timeScale = PlayerDeathHitStopTimeScale;
        await UniTask.Delay(TimeSpan.FromSeconds(PlayerDeathHitStopDurationSeconds), ignoreTimeScale: true);
        Time.timeScale = 1.0f;

        var fadeTask = UIPresenter.Instance != null
            ? UIPresenter.Instance.FadeOutAsync(PlayerDeathFadeDurationSeconds)
            : UniTask.CompletedTask;
        var fadeBgmTask = AudioManager.Instance != null
            ? AudioManager.Instance.FadeBGMAsync(PlayerDeathFadeDurationSeconds, CancellationToken.None)
            : UniTask.CompletedTask;

        await UniTask.WhenAll(fadeTask, fadeBgmTask);

        await SceneManager.LoadSceneAsync((int)GameScene.Preparing, LoadSceneMode.Additive).ToUniTask();

        if (SceneManager.GetSceneByBuildIndex((int)GameScene.MainGame).isLoaded)
        {
            await SceneManager.UnloadSceneAsync((int)GameScene.MainGame).ToUniTask();
        }
    }
}
