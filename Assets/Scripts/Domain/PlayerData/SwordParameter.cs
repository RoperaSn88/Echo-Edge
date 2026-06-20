public struct SwordParameter
{
    public int HP;
    public int Attack;
    public byte ReflectCount;

    public SwordParameter(int hp, int attack, byte reflectCount)
    {
        HP = hp;
        Attack = attack;
        ReflectCount = reflectCount;
    }
}