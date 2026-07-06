using Cysharp.Threading.Tasks;

public interface IDamageActivator
{
    public UniTask Damage();

    /// <summary>
    /// めちゃくちゃ早い一閃によるダメージ処理
    /// </summary>
    public UniTask FlashDamage();
}