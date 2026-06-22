public static class DefeatAllEnemiesStageClearTask
{
    public static void OnEnemyDead(int h, int w, int experience)
    {
        GameClearManager.UpdateLastEnemyPosition(h, w);
        GameClearManager.AddStageEarnedExperience(experience);
        GameClearManager.UpdateCondition();
    }
}
