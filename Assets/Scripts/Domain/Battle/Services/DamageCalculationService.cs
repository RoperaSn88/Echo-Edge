using UnityEngine;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// ダメージ計算を担うドメインサービス。
    /// 計算式をエンティティから分離することで、変更を一か所に集約する。
    /// </summary>
    public static class DamageCalculationService
    {
        /// <summary>
        /// コンボ1回あたりに増える、倍率の式へ渡す x の値
        /// </summary>
        private const float ComboStep = 0.2f;

        /// <summary>
        /// コンボ数からダメージ倍率を求める。
        /// x = 1 + 0.2 * (コンボ数 - 1) として f(x) = 2(x ln x - x) + 3。
        /// 傾きが 2 ln x になる式で、1コンボ目 (x = 1) の倍率が 1.0 になるよう定数項を 3 にしている。
        /// </summary>
        /// <param name="combo">現在のコンボ数 (1以上)</param>
        public static float CalculateComboMultiplier(int combo)
        {
            float x = 1.0f + (combo - 1) * ComboStep;
            return 2.0f * (x * Mathf.Log(x) - x) + 3.0f;
        }

        /// <summary>
        /// ダメージ量を計算する。
        /// 計算式: 攻撃力 - 防御力 / 2。結果が負になる場合は 0 を返す。
        /// </summary>
        /// <param name="attackPower">攻撃側の攻撃力</param>
        /// <param name="defenseValue">防御側の防御力</param>
        /// <returns>実際に与えるダメージ量（0以上）</returns>
        public static int Calculate(int attackPower, int defenseValue)
        {
            int damage = attackPower - defenseValue / 2;
            return damage < 0 ? 0 : damage;
        }
    }
}
