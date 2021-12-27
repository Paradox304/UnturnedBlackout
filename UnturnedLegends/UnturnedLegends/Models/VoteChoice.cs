using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedLegends.Enums;

namespace UnturnedLegends.Models
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
