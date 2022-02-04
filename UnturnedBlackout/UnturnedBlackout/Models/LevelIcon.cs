using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Models
{
    public class LevelIcon
    {
        public uint Rank { get; set; }
        public string IconLink { get; set; }
        public string IconLink28 { get; set; }
        public string IconLink54 { get; set; }

        public LevelIcon(uint rank, string iconLink, string iconLink28, string iconLink54)
        {
            Rank = rank;
            IconLink = iconLink;
            IconLink28 = iconLink28;
            IconLink54 = iconLink54;
        }

        public LevelIcon()
        {

        }
    }
}
