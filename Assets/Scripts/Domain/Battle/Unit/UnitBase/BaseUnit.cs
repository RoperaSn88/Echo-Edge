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
    public class BaseUnit: IEnemyUnit, IDamagable, IForcedMoveReceiver
    {
        [SerializeField]
        private UnitPosition _position;

        public int Height => _position.Height;
        public int Width => _position.Width;
        public UnitPosition Position => _position;

        private IUnitView _view;

        /// <summary>
        /// 紐づいている表示用 View
        /// </summary>
        protected IUnitView View => _view;

        /// <summary>
        /// マップのグリッドに登録せず、盤面の外に立つユニットか（『シーカー』など）。
        /// true の場合はマスを占有せず、ダメージも受けない。
        /// コンストラクタ内の Initialize から参照されるため、派生クラスでは定数を返すこと。
        /// </summary>
        protected virtual bool IsOffBoard => false;

        private BattleStatus _battleStatus;
        private IUnitAction _unitAction;
        private EnemyKinds _enemyKind = EnemyKinds.Invalid;

        /// <summary>
        /// マップ上で占有するマスのサイズ。
        /// LoadStatus が完了する前（マップへの初期登録時）にも必要になるため、
        /// コンストラクタで確定させておく（CSV読み込みは MapManager 側で先に行う）。
        /// </summary>
        private EnemySize _size;

        /// <summary>
        /// 移動できない残りフェーズ数。0 より大きい間はエネミーフェーズの移動をスキップする。
        /// </summary>
        private int _moveLockedPhases;

        /// <summary>
        /// マグネティックコアで飛行を解除されたときに移動できなくなるフェーズ数
        /// </summary>
        private const int MoveLockPhasesOnFlyingCancel = 1;

        public BaseUnit(int h, int w, EnemySize size = EnemySize.Default)
        {
            _size = size;
            Initialize(h, w);
        }

        /// <summary>
        /// CSV から エネミー ID に対応するステータスを読み込む
        /// </summary>
        /// <param name="enemyId">EnemyInfo.csv の ID</param>
        /// <param name="level">
        /// ステージごとの難易度調整に使うレベル。1 以上で HP / Attack / Defend に補正がかかる。
        /// 0 以下（既定）なら補正なし。
        /// </param>
        public async UniTask LoadStatus(EnemyKinds enemyId, int level = 0)
        {
            var status = await EnemyStatusLoader.TryLoad((int)enemyId, level);
            if (status == null)
            {
                Debug.LogWarning($"enemyId {(int)enemyId} のステータスを読み込めませんでした。デフォルトステータスで起動します。");
                return;
            }

            status.Initialize();
            // 盤面外のユニットは一閃の対象にならないため、念のため無敵にしておく
            if (IsOffBoard) status.IsInvincible = true;
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
            if (IsOffBoard)
            {
                MapManager.Instance.RegisterOffBoardUnit(this);
                return;
            }
            MapManager.Instance.RegisterUnit(this, h, w);
        }

        public UniTask Dead()
        {
            return Dead(_battleStatus.Experience);
        }

        /// <summary>
        /// 死亡処理を行う。
        /// </summary>
        /// <param name="experienceReward">撃破報酬として与える経験値。犠牲など報酬が発生しない死に方では 0 を渡す</param>
        protected async UniTask Dead(int experienceReward)
        {
            await _unitAction.Dead();

            var position = Position;
            // 死んだら自身のいるマスを空にする
            MapManager.Instance.RemoveUnitAt(position.Height, position.Width);

            // ドメインイベントをディスパッチして、アプリケーション層のハンドラーに通知する
            // (直接 DefeatAllEnemiesStageClearTask を呼ぶのではなく、イベント経由で疎結合にする)
            // クリア条件成立時はここでクリア演出・シナリオ再生の完了まで待機する。
            // 経験値が発生しない死に方でも、残り敵数の管理はイベント経由で行うためディスパッチ自体は必ず行う。
            await DomainEventDispatcher.Dispatch(new EnemyDefeatedEvent(position, experienceReward, _enemyKind));
        }

        public async UniTask Attack()
        {
            BattleManager.RegisterEnemy(_battleStatus);

            // 近距離(Width == 0)の敵はプレイヤーの手前まで踏み込んでから攻撃する
            var shouldApproach = Width == 0;
            if (shouldApproach)
            {
                await _view.ApproachPlayerForAttack();
            }

            try
            {
                // 踏み込み後の位置に対して、自分とプレイヤーが同時に映るようカメラ移動・メッセージ表示を行う
                await UniTask.WhenAll(
                    _view.WaitToCameraZoomWithPlayer(),
                    _unitAction.BeforeAttack()
                );

                await _view.WaitAttackAnim();

                await _unitAction.Attack();
            }
            finally
            {
                // 踏み込んでいた場合は元の位置へ戻る（攻撃が中断されても戻す）
                if (shouldApproach)
                {
                    await _view.ReturnFromApproach();
                }
            }
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

            // 飛行（溜め）中は一閃のダメージを受けない
            if (_unitAction is IFlyingUnit flyingAfterSpecific && _battleStatus != null)
            {
                _battleStatus.IsInvincible = flyingAfterSpecific.IsFlying;
            }
        }

        public async UniTask Act()
        {
            if (_unitAction == null) return;
            var pattern = await _unitAction.Act(Height, Width);
            switch (pattern)
            {
                case EnemyMoveKinds.Attack:
                    AudioManager.Instance.PlaySe(BattleSeType.EnemyTurn);
                    await Attack();
                    break;
                case EnemyMoveKinds.Specific:
                    AudioManager.Instance.PlaySe(BattleSeType.EnemyTurn);
                    await Specific();
                    break;
            }

            // 行動で出たダメージテキストを消す（攻撃せずに敵フェーズへ来た場合もフェード許可が立つように）
            UIPresenter.Instance.FadeTexts();
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
            if (_moveLockedPhases > 0)
            {
                // 落下中（移動不可）の間はこのフェーズの行動をすべてスキップし、落下状態を解除する
                _moveLockedPhases--;
                if (_moveLockedPhases == 0 && _view is IFlyingUnitView fallenView)
                {
                    fallenView.EndFallAnim();
                }
                return;
            }

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
            
            // 敵フェーズ中のダメージテキスト（被ダメージ等）は表示後すぐ消えるようにする。
            // 攻撃せずに防御・回復でターンを終えた場合はフェード許可が立っていないため、ここで立てておく。
            UIPresenter.Instance.FadeTexts();
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
            var mapH = mapManager.Height;
            var mapW = mapManager.Width;

            var dirH = new[] { 0, -1, 1, 0 };
            var dirW = new[] { -1, 0, 0, 1 };

            // 1. ゴール（左端）までの残り距離を、壁だけを障害物として求める（他のユニットは一時的なものなので無視）。
            //    以前は「左に1マス進むと+1」の点数で目的地を選んでいたため、前を塞がれると点数0の上下移動を選び、
            //    次のフェーズで逆向きに戻る、を繰り返して同じ場所に留まっていた（特に2x2の『でかエナー』）。
            var goalDist = ComputeGoalDistance(mapManager, span, mapH, mapW, dirH, dirW);
            var srcGoalDist = goalDist[srcH, srcW];
            // 壁に囲まれてゴールへの道が無い場合は、より左に行けるかどうかで判断する
            var useColumnAsGoal = srcGoalDist == int.MaxValue;
            int GoalMetric(int h, int w) => useColumnAsGoal ? w : goalDist[h, w];

            // 2. 現在の盤面（他のユニット・壁を含む）で、count 手以内に到達できるマスを幅優先探索で求める
            var stepTo = new int[mapH, mapW];
            var prevH = new int[mapH, mapW];
            var prevW = new int[mapH, mapW];
            for (var h = 0; h < mapH; h++)
            {
                for (var w = 0; w < mapW; w++)
                {
                    stepTo[h, w] = -1;
                    prevH[h, w] = -1;
                    prevW[h, w] = -1;
                }
            }

            var queue = new Queue<(int h, int w)>();
            stepTo[srcH, srcW] = 0;
            queue.Enqueue((srcH, srcW));
            while (queue.Count > 0)
            {
                var (h, w) = queue.Dequeue();
                if (stepTo[h, w] >= count) continue;

                for (var dir = 0; dir < dirH.Length; dir++)
                {
                    var nextH = h + dirH[dir];
                    var nextW = w + dirW[dir];
                    if (!mapManager.IsInBounds(nextH, nextW) || stepTo[nextH, nextW] >= 0) continue;
                    // 自身を除いた、占有する全マスの空き状況を確認する（2x2など複数マス対応）
                    if (!mapManager.IsFootprintFree(nextH, nextW, span, this)) continue;

                    stepTo[nextH, nextW] = stepTo[h, w] + 1;
                    prevH[nextH, nextW] = h;
                    prevW[nextH, nextW] = w;
                    queue.Enqueue((nextH, nextW));
                }
            }

            // 3. 到達できるマスのうち、ゴールに最も近づけるマスを目的地にする。
            //    今いる場所より近づけないなら移動しない（上下にうろうろして元の場所に戻るのを防ぐ）。
            //    同じ近さなら、手数が少ない → 縦のズレが小さい順に優先し、それでも並べばランダムに選ぶ。
            var bestMetric = GoalMetric(srcH, srcW);
            var dstH = -1;
            var dstW = -1;
            var tieCount = 0;
            for (var h = 0; h < mapH; h++)
            {
                for (var w = 0; w < mapW; w++)
                {
                    if (stepTo[h, w] <= 0) continue;

                    var metric = GoalMetric(h, w);
                    if (metric == int.MaxValue) continue;

                    var isBetter = metric < bestMetric ||
                        (dstH >= 0 && metric == bestMetric && (stepTo[h, w] < stepTo[dstH, dstW] ||
                            (stepTo[h, w] == stepTo[dstH, dstW] && Math.Abs(h - srcH) < Math.Abs(dstH - srcH))));
                    var isTie = dstH >= 0 && metric == bestMetric && stepTo[h, w] == stepTo[dstH, dstW] &&
                        Math.Abs(h - srcH) == Math.Abs(dstH - srcH);

                    if (isBetter)
                    {
                        bestMetric = metric;
                        dstH = h;
                        dstW = w;
                        tieCount = 1;
                    }
                    else if (isTie)
                    {
                        // 同点候補はリザーバーサンプリングで等確率に選ぶ
                        tieCount++;
                        if (UnityEngine.Random.Range(0, tieCount) == 0)
                        {
                            dstH = h;
                            dstW = w;
                        }
                    }
                }
            }

            if (dstH < 0) return;

            // 目的地から prev を逆にたどり、実際の移動経路を復元する。
            var path = new List<(int h, int w)>();
            var currentH = dstH;
            var currentW = dstW;
            while (currentH != srcH || currentW != srcW)
            {
                path.Add((currentH, currentW));
                var fromH = prevH[currentH, currentW];
                var fromW = prevW[currentH, currentW];
                if (fromH < 0 || fromW < 0) break;
                currentH = fromH;
                currentW = fromW;
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

        /// <summary>
        /// 各マスを基準マス（左上）にしたとき、左端（width=0）まで何手で行けるかを、壁だけを障害物として求める。
        /// 到達できないマスは int.MaxValue。
        /// </summary>
        private static int[,] ComputeGoalDistance(MapManager mapManager, int span, int mapH, int mapW, int[] dirH, int[] dirW)
        {
            var dist = new int[mapH, mapW];
            var queue = new Queue<(int h, int w)>();
            for (var h = 0; h < mapH; h++)
            {
                for (var w = 0; w < mapW; w++)
                {
                    dist[h, w] = int.MaxValue;
                }
            }

            for (var h = 0; h < mapH; h++)
            {
                if (!IsFootprintWithoutWall(mapManager, h, 0, span)) continue;
                dist[h, 0] = 0;
                queue.Enqueue((h, 0));
            }

            while (queue.Count > 0)
            {
                var (h, w) = queue.Dequeue();
                for (var dir = 0; dir < dirH.Length; dir++)
                {
                    var nextH = h + dirH[dir];
                    var nextW = w + dirW[dir];
                    if (!mapManager.IsInBounds(nextH, nextW) || dist[nextH, nextW] != int.MaxValue) continue;
                    if (!IsFootprintWithoutWall(mapManager, nextH, nextW, span)) continue;

                    dist[nextH, nextW] = dist[h, w] + 1;
                    queue.Enqueue((nextH, nextW));
                }
            }

            return dist;
        }

        /// <summary>
        /// anchorH/anchorW を左上とする span マス四方が、すべて盤面内かつ壁（building）が無いかを返す
        /// </summary>
        private static bool IsFootprintWithoutWall(MapManager mapManager, int anchorH, int anchorW, int span)
        {
            for (var dh = 0; dh < span; dh++)
            {
                for (var dw = 0; dw < span; dw++)
                {
                    var h = anchorH + dh;
                    var w = anchorW + dw;
                    if (!mapManager.IsInBounds(h, w)) return false;
                    if (mapManager.GetUnitAt(h, w) is building) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 指定したフェーズ数だけ、エネミーフェーズの移動をできなくする
        /// </summary>
        /// <param name="phases">移動できないフェーズ数</param>
        public void LockMove(int phases)
        {
            _moveLockedPhases = Math.Max(_moveLockedPhases, phases);
        }

        /// <inheritdoc/>
        public async UniTask OnPulledByMagneticCore()
        {
            // 飛行（溜め）中に引き寄せられた場合は、溜めと無敵を解除して着陸させ、次のフェーズは移動できなくする
            if (_unitAction is not IFlyingUnit flyingUnit || !flyingUnit.IsFlying) return;

            flyingUnit.CancelFlying();
            if (_battleStatus != null)
            {
                _battleStatus.IsInvincible = false;
            }
            LockMove(MoveLockPhasesOnFlyingCancel);

            if (_view is IFlyingUnitView flyingView)
            {
                await flyingView.WaitLandAnim();
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

            // 飛行中など無敵状態の間は一閃が当たらない
            if (_battleStatus.IsInvincible)
            {
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
        /// HPを回復し、その結果を View に反映する。
        /// </summary>
        /// <param name="amount">回復量</param>
        public async UniTask Heal(int amount)
        {
            if (_battleStatus == null) return;

            var before = _battleStatus.HP;
            _battleStatus.Heal(amount);

            // 最大HPで頭打ちになるため、実際に回復した量を反映する
            await ReflectHealToView(_battleStatus.HP - before);
        }

        /// <summary>
        /// ダメージ計算の結果を View に反映する（ダメージテキスト・HPゲージ・被弾／死亡アニメーション）。
        /// View が反映に対応していない場合は何もしない。
        /// </summary>
        /// <param name="result">反映するダメージ計算の結果</param>
        /// <param name="showEnergy">エナジー獲得演出を出すか。犠牲など報酬の対象外となる死に方では false を渡す</param>
        protected async UniTask ReflectDamageToView((int damage, bool isDeath) result, bool showEnergy = true)
        {
            if (!TryGetDamageReflectableView(out var damageView)) return;

            await damageView.ReflectDamage(result.damage, result.isDeath, _battleStatus, showEnergy);
        }

        /// <summary>
        /// 回復量を View に反映する（回復量テキスト・HPゲージ）。
        /// </summary>
        /// <param name="amount">実際に回復した量</param>
        private async UniTask ReflectHealToView(int amount)
        {
            if (amount <= 0) return;
            if (!TryGetDamageReflectableView(out var damageView)) return;

            await damageView.ReflectHeal(amount, _battleStatus);
        }

        /// <summary>
        /// 紐づく View が HP 変化の反映に対応しているかを判定する。
        /// </summary>
        private bool TryGetDamageReflectableView(out IDamageReflectableView damageView)
        {
            damageView = _view as IDamageReflectableView;
            if (damageView != null) return true;

            if (_view != null)
            {
                Debug.LogWarning($"{_view.GetType().Name} は IDamageReflectableView を実装していないため、HPの変化を View に反映できません。");
            }

            return false;
        }
    }
}
