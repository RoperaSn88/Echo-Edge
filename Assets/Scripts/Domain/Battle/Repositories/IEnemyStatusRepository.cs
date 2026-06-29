using Cysharp.Threading.Tasks;

/// <summary>
/// 敵ステータスの取得を抽象化するリポジトリインターフェース。
/// ドメイン層がインフラ層（CSVローダー等）に直接依存しないようにするための境界。
/// </summary>
public interface IEnemyStatusRepository
{
    /// <summary>
    /// 指定した敵種別のステータスを非同期で取得する。
    /// 取得できない場合は null を返す。
    /// </summary>
    UniTask<BattleStatus> LoadAsync(EnemyKinds enemyKind);
}
