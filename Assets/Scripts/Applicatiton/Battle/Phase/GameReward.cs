namespace Applicatiton.Battle.Phase
{
    public static class GameReward
    {
        /// <summary>
        /// 最後に倒した敵の高さ座標
        /// </summary>
        private static int _lastEnemyH;
        
        public static int LastEnemyH => _lastEnemyH;

        /// <summary>
        /// 最後に倒した敵の横座標
        /// </summary>
        private static int _lastEnemyW;
        
        
        public static int LastEnemyW => _lastEnemyW;
        
        /// <summary>
        /// ステージで獲得した経験値
        /// </summary>
        private static int _stageEarnedExperience;
        
        
        public static void UpdateLastEnemyPosition(int h, int w)
        {
            _lastEnemyH = h;
            _lastEnemyW = w;
        }

        public static void AddStageEarnedExperience(int value)
        {
            _stageEarnedExperience += value;
        }
        
        /// <summary>
        /// ステージで獲得した経験値をリセットする。ステージ開始時（StartPhase）から呼ぶ。
        /// ウェーブが切り替わっても経験値は引き継ぐため、ウェーブ切り替え時には呼ばない。
        /// </summary>
        public static void ResetStageExperience()
        {
            _stageEarnedExperience = 0;
        }
        
        public static (int gainedExperience, int currentExperience, int level) ApplyStageClearReward()
        {
            var playerStatus = BattleManager.PlayerStatus;
            if (playerStatus == null)
            {
                return (_stageEarnedExperience, 0, 1);
            }

            playerStatus.AddExperience(_stageEarnedExperience);
            playerStatus.LevelUp();
            PlayerSwordParameterHolder.SetPlayerProgress(playerStatus.Experience, playerStatus.Level);
            PlayerSwordParameterHolder.SetPlayerStatus(playerStatus);

            return (_stageEarnedExperience, playerStatus.Experience, playerStatus.Level);
        }
    }
}