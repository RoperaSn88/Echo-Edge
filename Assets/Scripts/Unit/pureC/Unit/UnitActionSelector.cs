namespace Unit.pureC.Unit
{
    public static class UnitActionSelector
    {
        /// <summary>
        /// 入力された列挙型からクラスを返す
        /// </summary>
        /// <param name="enemyType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IUnitAction SelectAction(EnemyKinds enemyType)
        {
            switch (enemyType)
            {
                case EnemyKinds.Builder:
                    return new Builder();
                case EnemyKinds.Skya:
                    return new Skya();
                case EnemyKinds.Booster:
                    return new Booster();
                
                default:
                    throw new System.ArgumentException($"Invalid UnitType: {enemyType}");
            }
        }
    }
}
