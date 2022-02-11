namespace UnturnedBlackout.Models.Level
{
    public class LevelIcon
    {
        public uint Level { get; set; }
        public string IconLink { get; set; }
        public string IconLink28 { get; set; }
        public string IconLink54 { get; set; }

        public LevelIcon(uint level, string iconLink, string iconLink28, string iconLink54)
        {
            Level = level;
            IconLink = iconLink;
            IconLink28 = iconLink28;
            IconLink54 = iconLink54;
        }

        public LevelIcon()
        {

        }
    }
}
