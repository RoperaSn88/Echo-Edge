using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

/// <summary>
/// ゲームクリアの演出を管理するクラス。
/// </summary>
public static class GameClearManager
{
    private static IStageClearObjective _stageClearObjective;
    private static StageClearConditionType _stageClearConditionType = StageClearConditionType.DefeatAllEnemies;

    /// <summary>
    /// 最後に倒した敵の高さ座標
    /// </summary>
    private static int _lastEnemyH;

    /// <summary>
    /// 最後に倒した敵の横座標
    /// </summary>
    private static int _lastEnemyW;

    /// <summary>
    /// ゲームクリア演出が開始済みか
    /// </summary>
    private static bool _isGameClearStarted;

    /// <summary>
    /// ステージで獲得した経験値
    /// </summary>
    private static int _stageEarnedExperience;

    /// <summary>
    /// ステージクリア条件に利用する値をキャッシュする。StartPhase から呼ぶ。
    /// </summary>
    /// <param name="conditionValue">ステージ開始時の条件値</param>
    public static void SetConditionValue(int conditionValue)
    {
        EnsureObjective();
        _stageClearObjective.Initialize(conditionValue);
        _isGameClearStarted = false;
        _stageEarnedExperience = 0;
        GameClearObjectivePresenter.Instance?.SetObjective(_stageClearObjective);
    }

    /// <summary>
    /// ステージクリア条件の進捗を更新する。
    /// </summary>
    public static void UpdateCondition()
    {
        EnsureObjective();
        if (_isGameClearStarted) return;

        if (_stageClearConditionType == StageClearConditionType.DefeatAllEnemies)
        {
            _stageClearObjective.UpdateCondition();
        }

        GameClearObjectivePresenter.Instance?.RefreshText();
    }

    public static void UpdateLastEnemyPosition(int h, int w)
    {
        _lastEnemyH = h;
        _lastEnemyW = w;
    }

    public static void AddStageEarnedExperience(int value)
    {
        _stageEarnedExperience += value;
    }

    public static bool GameClearCondition()
    {
        EnsureObjective();
        return _stageClearObjective.IsGameClearCondition();
    }

    public static bool GameClearInteraction()
    {
        EnsureObjective();
        return _stageClearObjective.GameClearInteraction();
    }

    public static void SetStageClearConditionType(StageClearConditionType conditionType)
    {
        _stageClearConditionType = conditionType;

        if (_stageClearObjective != null)
        {
            _stageClearObjective.OnGameClearInteraction -= TryStartGameClearSequence;
        }

        _stageClearObjective = CreateObjective(_stageClearConditionType);
        _stageClearObjective.OnGameClearInteraction += TryStartGameClearSequence;
        GameClearObjectivePresenter.Instance?.SetObjective(_stageClearObjective);
    }

    private static bool TryStartGameClearSequence()
    {
        if (_isGameClearStarted || !GameClearCondition())
        {
            return false;
        }

        _isGameClearStarted = true;
        StartGameClearSequenceAsync().Forget();
        return true;
    }

    private static void EnsureObjective()
    {
        if (_stageClearObjective == null)
        {
            SetStageClearConditionType(_stageClearConditionType);
        }
    }

    private static IStageClearObjective CreateObjective(StageClearConditionType conditionType)
    {
        switch (conditionType)
        {
            case StageClearConditionType.DefeatAllEnemies:
            default:
                return new DefeatAllEnemiesStageClearObjective();
        }
    }

    private static async UniTask StartGameClearSequenceAsync()
    {
        // 1. 暗転する
        await UIPresenter.Instance.FadeOutAsync(0.01f);

        // 2. ステータスのUIを非表示にする
        if (PlayerStatusPresenter.Instance != null)
        {
            PlayerStatusPresenter.Instance.gameObject.SetActive(false);
        }

        // 3. プレイヤーにカメラをより大きく瞬時に拡大し、カメラの角度をy軸30ほど傾ける
        if (PlayerController.Instance != null)
        {
            CameraManager.Instance.SetGameClearCamera(PlayerController.Instance.transform.position);
        }

        // 最後に倒した敵から下1マス、横3マスに壁が存在する場合は削除する
        RemoveWallsNearLastEnemy();

        // 4. 暗転をやめて表示する
        await UIPresenter.Instance.FadeInAsync(0.01f);
        var reward = ApplyStageClearReward();
        Time.timeScale = 0.0f;
        await GameClearRewardPresenter.Instance.ShowAsync(reward.level, reward.gainedExperience, reward.currentExperience);

        // 5. カメラを揺らす
        CameraManager.Instance.StartCameraShake();

        // クリックを待つ
        var mouseActions = new MouseClick();
        mouseActions.Enable();
        var tcs = new UniTaskCompletionSource();
        mouseActions.Mouse.MouseClick.started += _ => tcs.TrySetResult();
        await tcs.Task;
        mouseActions.Mouse.Disable();
        mouseActions.Dispose();

        // クリック時: カメラをもとの位置に戻して揺らすのをやめる
        CameraManager.Instance.StopCameraShake();
        var t1 = UIPresenter.Instance.FadeOutAsync(0.5f);
        var t2 = GameClearRewardPresenter.Instance.CloseAsync();
        var t3 = AudioManager.Instance != null
            ? AudioManager.Instance.FadeBGMAsync(0.5f, CancellationToken.None)
            : UniTask.CompletedTask;
        var t4 = AudioManager.Instance != null
            ? AudioManager.Instance.FadeAddedBGMAsync(0.5f, CancellationToken.None)
            : UniTask.CompletedTask;

        await UniTask.WhenAll(t1, t2, t3, t4);
        
        Time.timeScale = 1.0f;
        
        EnhancementManager.AddStone(1);

        // 6. MainGameをアンロードし、Preparingシーンを読み込む
        await SceneLoader.AdditiveLoadAsync(GameScene.Preparing);
        SceneLoader.Unload(GameScene.MainGame);
    }

    private static void RemoveWallsNearLastEnemy()
    {
        if (BuildingManager.Instance == null) throw new InvalidOperationException("BuildingManager.Instance が null です。");

        // 最後に倒した敵から下1マス、横3マスの壁を削除する
        for (int offsetW = -1; offsetW <= 1; offsetW++)
        {
            BuildingManager.Instance.TryRemoveWallAt(_lastEnemyH - 1, _lastEnemyW + offsetW);
        }
    }

    private static (int gainedExperience, int currentExperience, int level) ApplyStageClearReward()
    {
        var playerStatus = PlayerStatusPresenter.Instance?.PlayerBattleStatus;
        if (playerStatus == null)
        {
            return (_stageEarnedExperience, 0, 1);
        }

        playerStatus.AddExperience(_stageEarnedExperience);
        playerStatus.LevelUp();
        PlayerSwordParameterHolder.SetPlayerProgress(playerStatus.Experience, playerStatus.Level);
        PlayerSwordParameterHolder.SetPlayerStatus(playerStatus);

        return (_stageEarnedExperience, playerStatus.Experience, playerStatus.Level);
    }
}
