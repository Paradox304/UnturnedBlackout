using System.Collections.Generic;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class PlayerLoadout
    {
        public Dictionary<ushort, LoadoutGun> Guns { get; set; }
        public Dictionary<ushort, LoadoutKnife> Knives { get; set; }
        public Dictionary<int, GunSkin> GunSkinsSearchByID { get; set; }
        public Dictionary<int, KnifeSkin> KnifeSkinsSearchByID { get; set; }
        public Dictionary<ushort, List<GunSkin>> GunSkinsSearchByGunID { get; set; }
        public Dictionary<ushort, List<KnifeSkin>> KnifeSkinsSearchByKnifeID { get; set; }
        public Dictionary<ushort, GunSkin> GunSkinsSearchBySkinID { get; set; }
        public Dictionary<ushort, KnifeSkin> KnifeSkinsSearchBySkinID { get; set; }
        public Dictionary<int, LoadoutPerk> Perks { get; set; }
        public Dictionary<ushort, LoadoutGadget> Gadgets { get; set; }
        public Dictionary<int, LoadoutKillstreak> Killstreaks { get; set; }
        public Dictionary<int, LoadoutCard> Cards { get; set; }
        public Dictionary<ushort, LoadoutGlove> Gloves { get; set; }
        public Dictionary<int, Loadout> Loadouts { get; set; }

        public PlayerLoadout(Dictionary<ushort, LoadoutGun> guns, Dictionary<ushort, LoadoutKnife> knives, Dictionary<int, GunSkin> gunSkinsSearchByID, Dictionary<int, KnifeSkin> knifeSkinsSearchByID, Dictionary<ushort, List<GunSkin>> gunSkinsSearchByGunID, Dictionary<ushort, List<KnifeSkin>> knifeSkinsSearchByKnifeID, Dictionary<ushort, GunSkin> gunSkinsSearchBySkinID, Dictionary<ushort, KnifeSkin> knifeSkinsSearchBySkinID, Dictionary<int, LoadoutPerk> perks, Dictionary<ushort, LoadoutGadget> gadgets, Dictionary<int, LoadoutKillstreak> killstreaks, Dictionary<int, LoadoutCard> cards, Dictionary<ushort, LoadoutGlove> gloves, Dictionary<int, Loadout> loadouts)
        {
            Guns = guns;
            Knives = knives;
            GunSkinsSearchByID = gunSkinsSearchByID;
            KnifeSkinsSearchByID = knifeSkinsSearchByID;
            GunSkinsSearchByGunID = gunSkinsSearchByGunID;
            KnifeSkinsSearchByKnifeID = knifeSkinsSearchByKnifeID;
            GunSkinsSearchBySkinID = gunSkinsSearchBySkinID;
            KnifeSkinsSearchBySkinID = knifeSkinsSearchBySkinID;
            Perks = perks;
            Gadgets = gadgets;
            Killstreaks = killstreaks;
            Cards = cards;
            Gloves = gloves;
            Loadouts = loadouts;
        }
    }
}
