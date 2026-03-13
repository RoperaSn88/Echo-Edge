public interface IDamagable
{
    /// <summary>
    /// ダメージを受けるためのメソッド
    /// </summary>
    public (int damage, bool isDeath) Damage(int damage);
}