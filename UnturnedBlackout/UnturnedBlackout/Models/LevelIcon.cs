using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models
{
    public class LevelIcon
    {
        public int MinRank { get; set; }
        public int MaxRank { get; set; }
        public string IconLink { get; set; }

        public LevelIcon(int minRank, int maxRank, string iconLink)
        {
            MinRank = minRank;
            MaxRank = maxRank;
            IconLink = iconLink;
        }

        public LevelIcon()
        {

        }
    }
}
