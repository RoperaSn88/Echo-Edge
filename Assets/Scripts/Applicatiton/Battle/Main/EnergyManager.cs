namespace EchoEdge.App.Battle
{
    public static class EnergyManager
    {
        /// <summary>
        /// プレイヤーの所持エナジー 0~10
        /// </summary>
        private static int _currentEnergy;
        public static int CurrentEnergy => _currentEnergy;

        /// <summary>
        /// プレイヤーのエナジーゲージ 0~100
        /// </summary>
        private static int _currentEnergyGauge;
        public static int CurrentEnergyGauge => _currentEnergyGauge;

        private const int MaxEnergyGauge = 100;
        private const int MaxEnergy = 10;

        /// <summary>
        /// エナジーの所持数とゲージをリセットする。ゲーム開始時に呼ぶ。
        /// </summary>
        public static void Reset()
        {
            _currentEnergy = 10;
            _currentEnergyGauge = 0;
        }

        public static (float gaugeValue, int energyCount) AddEnergy(int energy)
        {
            if (_currentEnergy == MaxEnergy)
            {
                return (1f, MaxEnergy);
            }

            _currentEnergyGauge += energy;
            while(_currentEnergyGauge >= MaxEnergyGauge)
            {
                _currentEnergyGauge -= MaxEnergyGauge;
                if (_currentEnergy < MaxEnergy)
                {
                    _currentEnergy++;
                }
            }

            return (_currentEnergyGauge / 100f, _currentEnergy);
        }

        /// <summary>
        /// エナジーの所持数を直接増やす（ゲージは変化しない）。防御などで使う。最大数を超えた分は切り捨てる。
        /// </summary>
        /// <param name="count">増やすエナジーの個数</param>
        public static (float gaugeValue, int energyCount) AddEnergyCount(int count)
        {
            _currentEnergy = System.Math.Min(MaxEnergy, _currentEnergy + System.Math.Max(0, count));

            if (_currentEnergy == MaxEnergy)
            {
                return (1f, MaxEnergy);
            }

            return (_currentEnergyGauge / 100f, _currentEnergy);
        }

        public static (float gaugeValue, int energyCount) RemoveEnergy(int energy)
        {
            _currentEnergy -= energy;

            return (_currentEnergyGauge / 100f, _currentEnergy);
        }
    }
}
