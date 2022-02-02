using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models
{
    public class LevelIcon
    {
        public int Rank { get; set; }
        public string IconLink { get; set; }

        public LevelIcon(int rank, string iconLink)
        {
            Rank = rank;
            IconLink = iconLink;
        }

        public LevelIcon()
        {

        }
    }
}
