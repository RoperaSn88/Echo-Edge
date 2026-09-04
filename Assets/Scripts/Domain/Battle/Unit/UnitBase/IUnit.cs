using Cysharp.Threading.Tasks;

namespace EchoEdge.Domain.Battle
{
    public interface IUnit : IDamagable
    {
        /// <summary>
        /// 初期位置設定用
        /// </summary>
        /// <param name="h">縦方向の座標</param>
        /// <param name="w">横方向の座標</param>
        /// <returns></returns>
        public void Initialize(int h, int w);

        /// <summary>
        /// 攻撃
        /// </summary>
        public UniTask Attack();

        /// <summary>
        /// 移動できるか
        /// </summary>
        public bool CanMove();

        /// <summary>
        /// 移動
        /// </summary>
        /// <param name="h">移動する縦の大きさ</param>
        /// <param name="w">移動する横の大きさ</param>
        public UniTask Move(int h,int w);

        /// <summary>
        /// なんかの技
        /// たぶんViewでやるべき
        /// </summary>
        public UniTask Specific();

        public int GetMoveHeight();

        public int GetMoveWidth();
        public int GetHeight();
        public int GetWidth();

        public BattleStatus GetStatus();

        /// <summary>
        /// このユニットのエネミー種別を取得する（プレイヤーや壁など該当しない場合は Invalid）
        /// </summary>
        public EnemyKinds GetEnemyKind();

        /// <summary>
        /// このユニットがマップ上で占有するマスのサイズを取得する
        /// </summary>
        public EnemySize GetSize();

        /// <summary>
        /// 防御力・無敵状態を無視して直接HPを消費する（スキル発動コストなど、被ダメージではない自傷用）
        /// </summary>
        public UniTask<(int damage, bool isDeath)> ConsumeHP(int amount);

        /// <summary>
        /// ターン開始時の行動
        /// </summary>
        public UniTask OnTurnStart();

        /// <summary>
        /// ターン終了時の行動
        /// </summary>
        public UniTask OnTurnEnd();

        /// <summary>
        /// そのユニットの行動を定義する
        /// この中で移動、攻撃を行う。
        /// </summary>
        /// <returns></returns>
        public UniTask MoveTurn();
    }

    public interface IEnemyUnit : IUnit
    {
    }
}
