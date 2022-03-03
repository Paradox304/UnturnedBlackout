using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutKnifeSkin
    {
        public KnifeSkin Skin { get; set; }
        public bool IsEquipped { get; set; }

        public LoadoutKnifeSkin(KnifeSkin skin, bool isEquipped)
        {
            Skin = skin;
            IsEquipped = isEquipped;
        }
    }
}
