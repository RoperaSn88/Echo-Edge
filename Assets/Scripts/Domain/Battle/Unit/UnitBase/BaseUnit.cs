using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unit.pureC.Unit;


[System.Serializable]
public class BaseUnit: IEnemyUnit, IDamagable
{
    [SerializeField]
    private int height;
    public int Height => height;

    [SerializeField]
    private int width;
    public int Width => width;

    [SerializeField]
    private int moveHeight;
    public int MoveHeight => moveHeight;

    [SerializeField]
    private int moveWidth;
    public int MoveWidth => moveWidth;

    private IUnitView _view;

    private BattleStatus _battleStatus;
    private IUnitAction _unitAction;

    public BaseUnit(int h, int w)
    {
        Initialize(h, w);
    }

    /// <summary>
    /// CSV から エネミー ID に対応するステータスを読み込む
    /// </summary>
    /// <param name="enemyId">EnemyInfo.csv の ID</param>
    public async UniTask LoadStatus(EnemyKinds enemyId)
    {
        var status = new BattleStatus();
        bool loaded = await EnemyStatusLoader.TryLoad((int)enemyId, status);
        if (!loaded)
        {
            Debug.LogWarning($"enemyId {(int)enemyId} のステータスを読み込めませんでした。デフォルトステータスで起動します。");
        }
        status.Initialize();
        _battleStatus = status;
        
        // ユニット特有のアクションを行う
        _unitAction = UnitActionSelector.SelectAction(enemyId);
    }

    /// <summary>
    /// 表示用 View を紐づけ、UnitAction を生成する
    /// </summary>
    /// <param name="view">対応する IUnitView</param>
    public void SetView(IUnitView view)
    {
        _view = view;
    }

    public async void Initialize(int h, int w)
    {
        height = h;
        width = w;
        await UniTask.WaitUntil(()=> MapManager.Instance);
        MapManager.Instance.RegisterUnit(this,h,w);
    }

    public async UniTask Dead()
    {
        await _unitAction.Dead();
        
        var h = Height;
        var w = Width;
        // 死んだら自身のいるマスを空にする
        MapManager.Instance.RemoveUnitAt(h, w);
        // ゲームクリア判定
        DefeatAllEnemiesStageClearTask.OnEnemyDead(h, w, _battleStatus.Experience);
    }

    public async UniTask Attack()
    {
        BattleManager.RegisterEnemy(_battleStatus);
        await UniTask.WhenAll(
            _view.WaitToCameraZoom(),
            _unitAction.BeforeAttack()
        );

        await _view.WaitAttackAnim();
        
        await _unitAction.Attack();
    }

    public async UniTask Specific()
    {
        BattleManager.RegisterEnemy(_battleStatus);

        UniTask action;
        if (_unitAction is IFlyingUnit flyingUnit1)
        {
            if(flyingUnit1.IsFlying) action = flyingUnit1.WaitFlyingMessage();
            else action = flyingUnit1.WaitToFlyMessage();
        }
        else
        {
            action = _unitAction.BeforeSpecific();
        }
        
        await UniTask.WhenAll(
            _view.WaitToCameraZoom(),
            action
        );

        // 飛行可能なユニットは飛行状態に応じてアニメーションを切り替える。
        // IsFlying が true（既に飛行中）→ ビームアニメ、false（地上）→ 飛び上がりアニメ
        if (_unitAction is IFlyingUnit flyingUnit)
        {
            if (_view is IFlyingUnitView flyingView)
            {
                if (flyingUnit.IsFlying)
                {
                    await flyingView.WaitAnimAfterFlying();
                }
                else
                {
                    await flyingView.WaitFlyAnim();
                }
            }
            else
            {
                Debug.LogWarning($"{_view.GetType().Name} は IFlyingUnitView を実装していません。WaitSpecificAnim にフォールバックします。");
                await _view.WaitSpecificAnim();
            }
        }
        else
        {
            await _view.WaitSpecificAnim();
        }
        
        await _unitAction.Specific(Height, Width);
    }

    public async UniTask Act()
    {
        if (_unitAction == null) return;
        var pattern = await _unitAction.Act(Height, Width);
        switch (pattern)
        {
            case EnemyMoveKinds.Attack:
                AudioManager.Instance.PlaySe(SeAudioType.EnemyTurn);
                await Attack();
                break;
            case EnemyMoveKinds.Specific:
                AudioManager.Instance.PlaySe(SeAudioType.EnemyTurn);
                await Specific();
                break;
        }
    }

    public async UniTask OnTurnStart()
    {
        _view.FadeGauge(0f).Forget();
        _battleStatus?.TickBuffs();
        if (_unitAction == null) return;
        await _unitAction.OnTurnStart();
    }

    public async UniTask OnTurnEnd()
    {
        _view.FadeGauge(1f).Forget();
        if (_unitAction == null) return;
        await _unitAction.OnTurnEnd();
    }

    public bool CanMove()
    {
        return true;
    }

    public async UniTask MoveTurn()
    {
        if (_battleStatus.MovePattern == MovePattern.Before)
        {
            // 攻撃をするが、遠距離か近距離かで攻撃するか変更する
            await Act();
            await MessagePresenter.Instance.DisappearMessage();
        }
        
        // 移動
        try
        {
            // もし飛行中ならナシ
            if (_unitAction is IFlyingUnit fly)
            {
                if(!fly.IsFlying) await TryMoveByScoreMap(_battleStatus.Move);
            }
            else
            {
                await TryMoveByScoreMap(_battleStatus.Move);
            }
        }
        catch
        {
            
        }
        
        if (_battleStatus.MovePattern == MovePattern.After)
        {
            // 行動をする
            // いったん攻撃か
            await Act();
            await MessagePresenter.Instance.DisappearMessage();
        }
    } 

    private async UniTask TryMoveByScoreMap(int count)
    {
        if (count <= 0) return;

        // 一番左ならなし
        if (Width <= 0) return;

        var mapManager = MapManager.Instance;
        var srcH = Height;
        var srcW = Width;
        if (!mapManager.IsInBounds(srcH, srcW)) return;

        var mapSize = count * 2 + 1;
        var scoreMap = new byte[mapSize, mapSize];
        var minScore = int.MinValue / 4;
        var scoreByStep = new int[count + 1, mapManager.Height, mapManager.Width];
        var prevH = new int[count + 1, mapManager.Height, mapManager.Width];
        var prevW = new int[count + 1, mapManager.Height, mapManager.Width];
        var offset = count;

        // DP テーブルを初期化する。
        // scoreByStep: 「step 手でそのマスに到達したときの最大スコア」
        // prevH / prevW: その状態に到達する直前マス（経路復元用）
        for (var step = 0; step <= count; step++)
        {
            for (var h = 0; h < mapManager.Height; h++)
            {
                for (var w = 0; w < mapManager.Width; w++)
                {
                    scoreByStep[step, h, w] = minScore;
                    prevH[step, h, w] = -1;
                    prevW[step, h, w] = -1;
                }
            }
        }

        scoreByStep[0, srcH, srcW] = 0;

        // 4 方向の移動定義。
        // 左を強く優先する評価になっており、右移動にはペナルティを与える。
        // 使うには、dirHとdirWに同じindexでアクセスする。例えば、dirH[0], dirW[0] は「上に移動」を表す。
        var dirH = new[] { 0, -1, 1, 0 };
        var dirW = new[] { -1, 0, 0, 1 };
        var dirScore = new[] { 2, 1, 1, -1 };

        // 手数を 1 ずつ進めながら、到達可能マスの最大スコアを更新する。
        // 同時に「どこから来たか」を prev 配列に記録し、あとで経路復元できるようにする。
        for (var step = 1; step <= count; step++)
        {
            for (var h = 0; h < mapManager.Height; h++)
            {
                for (var w = 0; w < mapManager.Width; w++)
                {
                    var baseScore = scoreByStep[step - 1, h, w];
                    if (baseScore == minScore) continue;

                    for (var dir = 0; dir < dirH.Length; dir++)
                    {
                        var nextH = h + dirH[dir];
                        var nextW = w + dirW[dir];
                        if (!mapManager.IsInBounds(nextH, nextW)) continue;
                        if (mapManager.GetUnitAt(nextH, nextW) != null) continue;

                        var candidate = baseScore + dirScore[dir];
                        if (candidate <= scoreByStep[step, nextH, nextW]) continue;

                        scoreByStep[step, nextH, nextW] = candidate;
                        prevH[step, nextH, nextW] = h;
                        prevW[step, nextH, nextW] = w;
                    }
                }
            }
        }

        var hasDestination = false;
        var bestStep = -1;
        var dstH = srcH;
        var dstW = srcW;
        var bestScore = minScore;

        // 全 step の候補を走査して最終目的地を選ぶ。
        // 同スコア時は「より左」「縦距離が小さい」位置を優先する。
        // scoreMap は可視化・デバッグ用のローカル評価マップとして更新する。
        for (var step = 1; step <= count; step++)
        {
            for (var h = 0; h < mapManager.Height; h++)
            {
                for (var w = 0; w < mapManager.Width; w++)
                {
                    var score = scoreByStep[step, h, w];
                    if (score == minScore) continue;

                    var localH = h - srcH + count;
                    var localW = w - srcW + count;
                    if (localH >= 0 && localH < mapSize && localW >= 0 && localW < mapSize)
                    {
                        var stored = score + offset;
                        if (stored < 0) stored = 0;
                        if (stored > byte.MaxValue) stored = byte.MaxValue;
                        if (scoreMap[localH, localW] < stored) scoreMap[localH, localW] = (byte)stored;
                    }

                    if (!hasDestination || score > bestScore ||
                        (score == bestScore && (w < dstW || (w == dstW && Math.Abs(h - srcH) < Math.Abs(dstH - srcH)))))
                    {
                        hasDestination = true;
                        bestScore = score;
                        bestStep = step;
                        dstH = h;
                        dstW = w;
                    }
                }
            }
        }

        if (!hasDestination) return;

        // 目的地から prev を逆にたどり、実際の移動経路を復元する。
        var path = new List<(int h, int w)>();
        var currentH = dstH;
        var currentW = dstW;
        var currentStep = bestStep;

        while (currentStep > 0)
        {
            path.Add((currentH, currentW));
            var fromH = prevH[currentStep, currentH, currentW];
            var fromW = prevW[currentStep, currentH, currentW];
            if (fromH < 0 || fromW < 0) break;
            currentH = fromH;
            currentW = fromW;
            currentStep--;
        }

        // 復元した経路を先頭から順に実行し、途中で失敗したら打ち切る。
        path.Reverse();
        foreach (var waypoint in path)
        {
            if (!await mapManager.TryMoveUnitTo(this, waypoint.h, waypoint.w))
            {
                break;
            }
        }
    }

    public async UniTask Move(int y, int x)
    {
        height = y;
        width = x;
        await _view.Move(y, x);
    }

    public int GetMoveHeight()
    {
        return Height;
    }

    public int GetMoveWidth()
    {
        return Width;
    }

    public int GetHeight()
    {
        return Height;
    }

    public int GetWidth()
    {
        return Width;
    }

    public BattleStatus GetStatus()
    {
        return _battleStatus;
    }

    public async UniTask<(int damage, bool isDeath)> Damage(int damage)
    {
        var result = await _battleStatus.Damage(damage);
        
        if (result.isDeath)
        {
            await Dead();
        }
            
        return result;
    }
}
