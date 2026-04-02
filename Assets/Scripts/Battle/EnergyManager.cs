public static class EnergyManager
{
    private static int _currentEnergy;
    public static int CurrentEnergy => _currentEnergy;
    
    private static int _currentEnergyGauge;
    public static int CurrentEnergyGauge => _currentEnergyGauge;
    
    private const int MaxEnergyGauge = 100;
    private const int MaxEnergy = 10;
    
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
}