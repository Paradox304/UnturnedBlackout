using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Models.Global
{
    public class VoteChoice
    {
        public ArenaLocation Location { get; set; }
        public EGameType GameMode { get; set; }

        public VoteChoice(ArenaLocation location, EGameType gameMode)
        {
            Location = location;
            GameMode = gameMode;
        }
    }
}
