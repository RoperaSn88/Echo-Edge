namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 敵のステータスを読み込んで初期化した直後に行う処理。
    /// EnemyInitializeActivator に登録すると、BaseUnit.LoadStatus から呼ばれる。
    /// </summary>
    public interface IEnemyInitializeAction
    {
        /// <param name="enemyKind">初期化した敵の種類</param>
        /// <param name="status">初期化した敵のステータス</param>
        public void OnEnemyInitialize(EnemyKinds enemyKind, BattleStatus status);
    }
}
