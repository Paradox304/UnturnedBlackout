using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Enums;
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
