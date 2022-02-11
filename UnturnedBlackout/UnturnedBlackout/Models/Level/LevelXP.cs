namespace UnturnedBlackout.Models.Level
{
    public class LevelXP
    {
        public uint Level { get; set; }
        public int XPNeeded { get; set; }

        public LevelXP()
        {

        }

        public LevelXP(uint level, int xPNeeded)
        {
            Level = level;
            XPNeeded = xPNeeded;
        }
    }
}
