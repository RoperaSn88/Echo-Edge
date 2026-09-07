using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Infra.Audio;
using EchoEdge.Infra.Battle;
using EchoEdge.Presenter.Battle;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Battle
{
    [System.Serializable]
    public class BaseUnit: IEnemyUnit, IDamagable
    {
        [SerializeField]
        private UnitPosition _position;

        public int Height => _position.Height;
        public int Width => _position.Width;
        public UnitPosition Position => _position;

        private IUnitView _view;

        private BattleStatus _battleStatus;
        private IUnitAction _unitAction;
        private EnemyKinds _enemyKind = EnemyKinds.Invalid;

        /// <summary>
        /// マップ上で占有するマスのサイズ。
        /// LoadStatus が完了する前（マップへの初期登録時）にも必要になるため、
        /// コンストラクタで確定させておく（CSV読み込みは MapManager 側で先に行う）。
        /// </summary>
        private EnemySize _size;

        public BaseUnit(int h, int w, EnemySize size = EnemySize.Default)
        {
            _size = size;
            Initialize(h, w);
        }

        /// <summary>
        /// CSV から エネミー ID に対応するステータスを読み込む
        /// </summary>
        /// <param name="enemyId">EnemyInfo.csv の ID</param>
        public async UniTask LoadStatus(EnemyKinds enemyId)
        {
            var status = await EnemyStatusLoader.TryLoad((int)enemyId);
            if (status == null)
            {
                Debug.LogWarning($"enemyId {(int)enemyId} のステータスを読み込めませんでした。デフォルトステータスで起動します。");
                return;
            }

            status.Initialize();
            _battleStatus = status;
            _enemyKind = enemyId;
            _size = status.Size;

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
            _position = new UnitPosition(h, w);
            await UniTask.WaitUntil(() => MapManager.Instance);
            MapManager.Instance.RegisterUnit(this, h, w);
        }

        public async UniTask Dead()
        {
            await _unitAction.Dead();

            var position = Position;
            // 死んだら自身のいるマスを空にする
            MapManager.Instance.RemoveUnitAt(position.Height, position.Width);

            // ドメインイベントをディスパッチして、アプリケーション層のハンドラーに通知する
            // (直接 DefeatAllEnemiesStageClearTask を呼ぶのではなく、イベント経由で疎結合にする)
            // クリア条件成立時はここでクリア演出・シナリオ再生の完了まで待機する。
            await DomainEventDispatcher.Dispatch(new EnemyDefeatedEvent(position, _battleStatus.Experience));
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
            if (Position.IsLeftmost) return;

            var mapManager = MapManager.Instance;
            var srcH = Height;
            var srcW = Width;
            if (!mapManager.IsInBounds(srcH, srcW)) return;

            // 2x2など複数マスを占有するユニットは、移動先候補全マスの空き状況を見る必要がある
            var span = (int)GetSize();

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
                            // 自身を除いた、占有する全マスの空き状況を確認する（2x2など複数マス対応）
                            if (!mapManager.IsFootprintFree(nextH, nextW, span, this)) continue;

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
            _position = new UnitPosition(y, x);
            await _view.Move(y, x);
        }

        public int GetMoveHeight() => Height;
        public int GetMoveWidth() => Width;
        public int GetHeight() => Height;
        public int GetWidth() => Width;

        public BattleStatus GetStatus()
        {
            return _battleStatus;
        }

        public EnemyKinds GetEnemyKind() => _enemyKind;

        public EnemySize GetSize() => _size;

        public async UniTask<(int damage, bool isDeath)> Damage(int damage)
        {
            var result = await _battleStatus.Damage(damage);

            await ReflectDamageToView(result);

            if (result.isDeath)
            {
                await Dead();
            }

            return result;
        }

        public async UniTask<(int damage, bool isDeath)> ConsumeHP(int amount)
        {
            var result = await _battleStatus.ConsumeHP(amount);

            await ReflectDamageToView(result);

            if (result.isDeath)
            {
                await Dead();
            }

            return result;
        }

        /// <summary>
        /// 自身を対象としたダメージ計算を BaseUnit 側から発火する。
        /// View の OnTriggerEnter を起点とする既存の経路とは別に、
        /// ドメイン側からでも通常攻撃と同じ計算（コンボ・反射・QTE 倍率込み）を行えるようにする。
        /// </summary>
        /// <param name="attackTypeRate">攻撃種類ごとのダメージ倍率</param>
        /// <returns>(与えたダメージ量, 死亡したか)</returns>
        public UniTask<(int damage, bool isDeath)> ActivateDamage(float attackTypeRate = 1.0f)
        {
            return ActivateDamage(() => BattleManager.EnemyDamage(attackTypeRate));
        }

        /// <summary>
        /// 自身を対象とした「めちゃくちゃ早い一閃」のダメージ計算を BaseUnit 側から発火する。
        /// </summary>
        /// <param name="attackTypeRate">攻撃種類ごとのダメージ倍率</param>
        /// <returns>(与えたダメージ量, 死亡したか)</returns>
        public UniTask<(int damage, bool isDeath)> ActivateFlashDamage(float attackTypeRate = 1.0f)
        {
            return ActivateDamage(() => BattleManager.FlashAttackDamage(attackTypeRate));
        }

        /// <summary>
        /// ダメージ計算を実行し、その結果を View に反映したうえで死亡処理まで行う。
        /// </summary>
        /// <param name="calculateDamage">実行するダメージ計算</param>
        private async UniTask<(int damage, bool isDeath)> ActivateDamage(Func<UniTask<(int damage, bool isDeath)>> calculateDamage)
        {
            if (_battleStatus == null)
            {
                Debug.LogWarning("ステータスが読み込まれていないため、ダメージ計算を発火できません。");
                return (0, false);
            }

            // BattleManager のダメージ計算対象を自身に切り替えてから計算させる
            BattleManager.RegisterEnemy(_battleStatus);
            var result = await calculateDamage();

            await ReflectDamageToView(result);

            if (result.isDeath)
            {
                await Dead();
            }

            return result;
        }

        /// <summary>
        /// ダメージ計算の結果を View に反映する（ダメージテキスト・HPゲージ・被弾／死亡アニメーション）。
        /// View が反映に対応していない場合は何もしない。
        /// </summary>
        /// <param name="result">反映するダメージ計算の結果</param>
        private async UniTask ReflectDamageToView((int damage, bool isDeath) result)
        {
            if (_view == null) return;

            if (_view is IDamageReflectableView damageView)
            {
                await damageView.ReflectDamage(result.damage, result.isDeath, _battleStatus);
                return;
            }

            Debug.LogWarning($"{_view.GetType().Name} は IDamageReflectableView を実装していないため、ダメージ結果を View に反映できません。");
        }
    }
}
