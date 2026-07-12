using Cysharp.Threading.Tasks;

public interface IDamageActivator
{
    public UniTask Damage();

    /// <summary>
    /// めちゃくちゃ早い一閃によるダメージ処理
    /// </summary>
    /// <returns>この攻撃で対象を撃破したかどうか</returns>
    public UniTask<bool> FlashDamage();
}