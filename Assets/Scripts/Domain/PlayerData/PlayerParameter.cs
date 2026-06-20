public struct PlayerParameter
{
    public int HP;
    public int Attack;
    public int Defend;
    public int Experience;
    public int Level;

    public PlayerParameter(int hp, int attack, int defend, int experience = 0, int level = 1)
    {
        HP = hp;
        Attack = attack;
        Defend = defend;
        Experience = experience;
        Level = level;
    }
}