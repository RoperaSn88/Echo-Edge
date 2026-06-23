public class Experiences
{
    public int Experience { get; private set; }
    public int Level { get; private set; }
    private const int ExperiencePerLevel = 100;

    public Experiences(int experience, int level = 0)
    {
        Experience = experience;
        Level = level;
    }

    public void AddExperience(int amount)
    {
        Experience += amount;
        while (Experience >= ExperiencePerLevel)
        {
            Experience -= ExperiencePerLevel;
            Level++;
        }
    }
}