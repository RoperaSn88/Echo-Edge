namespace EchoEdge.Domain.Battle
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
                case EnemyKinds.Brute:
                    return new Brute();
                case EnemyKinds.Enar:
                    return new Enar();
                case EnemyKinds.BigEnar:
                    return new BigEnar();

                default:
                    throw new System.ArgumentException($"Invalid UnitType: {enemyType}");
            }
        }
    }
}
