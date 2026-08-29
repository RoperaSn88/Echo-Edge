using System;
using System.Threading;
using Applicatiton.Battle.Phase;
using Cysharp.Threading.Tasks;
using Domain.Phase.GameClear;
using UI;
using UnityEngine;

/// <summary>
/// ゲームクリアの演出を管理するクラス。
/// </summary>
public static class GameClearManager
{
    private static IStageClearTask _currentTask;
    private static bool _isClear;
    public static bool IsClear => _isClear;
    
    private static bool _gameClearAsyncStarted;

    /// <summary>
    /// このステージで使うクリア条件タスクを切り替える。ステージ開始時（StartPhase）から呼ぶ。
    /// 直前のタスクの購読を解除し、新しいタスクの購読を開始する。
    /// </summary>
    public static void SetStageClearConditionType(StageClearConditionType conditionType)
    {
        _currentTask?.Unsubscribe();
        _currentTask = CreateTask(conditionType);
        _currentTask.Subscribe();
        _isClear = false;
    }

    /// <summary>
    /// ステージ（ウェーブ）開始時に、現在アクティブなタスクへ条件の初期値を渡す。
    /// </summary>
    public static void Initialize(int conditionValue)
    {
        _currentTask?.Initialize(conditionValue);
    }

    private static IStageClearTask CreateTask(StageClearConditionType conditionType)
    {
        switch (conditionType)
        {
            case StageClearConditionType.Endurance:
                return new EndureStageClearTask();
            case StageClearConditionType.DefeatAllEnemies:
            default:
                return new DefeatAllEnemiesStageClearTask();
        }
    }

    /// <summary>
    /// ステージクリア条件の進捗表示を更新する。
    /// </summary>
    public static void UpdateText(string context, int value)
    {
        GameClearConditionView.Instance.RefreshText(context, value);
    }

    public static void SetStageClearCondition(bool isClear)
    {
        _isClear = isClear;
    }

    public static bool GameClearCondition()
    {
        if (IsClear && WaveManager.HasNextWave) return true;
        return false;
    }

    public static async UniTask StartGameClearSequenceAsync()
    {
        if(_gameClearAsyncStarted) return;
        _gameClearAsyncStarted = true;

        // ステージクリアの進行状況を記録し、次のステージを選択可能にする
        StageData.RegisterStageCleared(StageData.Level);

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
        var reward = GameReward.ApplyStageClearReward();
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
            BuildingManager.Instance.TryRemoveWallAt(GameReward.LastEnemyH - 1, GameReward.LastEnemyW + offsetW);
        }
    }
}
