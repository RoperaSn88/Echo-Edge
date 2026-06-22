using Cysharp.Threading.Tasks;

/// <summary>
/// 装備品の効果を発揮するためのインターフェース
/// </summary>
public interface IEquipEffect
{
    /// <summary>
    /// 装備品の効果を発揮する
    /// </summary>
    public UniTask Activate();
}
