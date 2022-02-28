using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class PlayerLoadout
    {
        public Dictionary<ushort, LoadoutGun> Guns { get; set; }
        public Dictionary<ushort, LoadoutKnife> Knives { get; set; }
        public Dictionary<int, GunSkin> GunSkins { get; set; }
        public Dictionary<int, KnifeSkin> KnifeSkins { get; set; }
        public Dictionary<int, LoadoutPerk> Perks { get; set; }
        public Dictionary<int, LoadoutGadget> Gadgets { get; set; }
        public Dictionary<int, LoadoutKillstreak> Killstreaks { get; set; }
        public Dictionary<int, LoadoutCard> Cards { get; set; }
        public Dictionary<int, LoadoutGlove> Gloves { get; set; }
        public Dictionary<int, Loadout> Loadouts { get; set; }

        public PlayerLoadout(Dictionary<ushort, LoadoutGun> guns, Dictionary<ushort, LoadoutKnife> knives, Dictionary<int, GunSkin> gunSkins, Dictionary<int, KnifeSkin> knifeSkins, Dictionary<int, LoadoutPerk> perks, Dictionary<int, LoadoutGadget> gadgets, Dictionary<int, LoadoutKillstreak> killstreaks, Dictionary<int, LoadoutCard> cards, Dictionary<int, LoadoutGlove> gloves, Dictionary<int, Loadout> loadouts)
        {
            Guns = guns;
            Knives = knives;
            GunSkins = gunSkins;
            KnifeSkins = knifeSkins;
            Perks = perks;
            Gadgets = gadgets;
            Killstreaks = killstreaks;
            Cards = cards;
            Gloves = gloves;
            Loadouts = loadouts;
        }
    }
}
