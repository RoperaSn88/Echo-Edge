namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// EnemyKinds に対応するユニットを生成するファクトリ。
    /// 固有の状態や死に方を持つ敵だけ BaseUnit の派生クラスを返し、それ以外は BaseUnit を返す。
    /// ターン中の振る舞い（IUnitAction）の選択は UnitActionSelector が担当する。
    /// </summary>
    public static class UnitFactory
    {
        /// <summary>
        /// エネミー種別に応じたユニットを生成する
        /// </summary>
        /// <param name="enemyKind">生成するエネミーの種別</param>
        /// <param name="h">配置する縦座標</param>
        /// <param name="w">配置する横座標</param>
        /// <param name="size">マップ上で占有するマスのサイズ</param>
        public static BaseUnit Create(EnemyKinds enemyKind, int h, int w, EnemySize size = EnemySize.Default)
        {
            switch (enemyKind)
            {
                case EnemyKinds.Enar:
                    return new EnarUnit(h, w, size);

                default:
                    return new BaseUnit(h, w, size);
            }
        }
    }
}
