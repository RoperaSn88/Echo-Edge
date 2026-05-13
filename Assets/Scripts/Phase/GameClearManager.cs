using System;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

/// <summary>
/// ゲームクリアの演出を管理するクラス。
/// </summary>
public static class GameClearManager
{
    /// <summary>
    /// 残り敵ユニット数
    /// </summary>
    private static int _enemyCount;

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
    /// 敵の数をキャッシュする。StartPhase から呼ぶ。
    /// </summary>
    /// <param name="count">ステージ開始時の敵ユニット数</param>
    public static void SetEnemyCount(int count)
    {
        _enemyCount = count;
        _isGameClearStarted = false;
        _stageEarnedExperience = 0;
    }

    /// <summary>
    /// 敵が死亡したときに呼ぶ。カウントを減らし、0になればゲームクリア演出を開始する。
    /// </summary>
    /// <param name="h">死亡した敵の高さ座標</param>
    /// <param name="w">死亡した敵の横座標</param>
    public static void OnEnemyDead(int h, int w, int experience)
    {
        Debug.Log("EnemyEnd");
        if (_isGameClearStarted) return;

        _lastEnemyH = h;
        _lastEnemyW = w;
        _stageEarnedExperience += experience;
        _enemyCount--;

        if (_enemyCount <= 0)
        {
            _isGameClearStarted = true;
            StartGameClearSequenceAsync().Forget();
        }
    }

    private static async UniTask StartGameClearSequenceAsync()
    {
        // 1. 暗転する
        await UIPresenter.Instance.FadeOutAsync();

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
        await UIPresenter.Instance.FadeInAsync();
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
        await CameraManager.Instance.ActResetCameraTarget();
        Time.timeScale = 1.0f;

        // 6. MainGameをアンロードし、Preparingシーンを読み込む
        SceneLoader.Load(GameScene.Preparing);
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

        return (_stageEarnedExperience, playerStatus.Experience, playerStatus.Level);
    }
}
