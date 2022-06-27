using System.Collections.Generic;
using UnturnedBlackout.Models.Global;

namespace UnturnedBlackout.Models.Configuration
{
    public class GamemodeConfig
    {
        public List<GamemodeOption> GamemodeOptions { get; set; }

        public GamemodeConfig()
        {
            GamemodeOptions = new();
        }
    }
}
