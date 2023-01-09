using System;
using System.Collections.Generic;
using Steamworks;
using UnturnedBlackout.Database.Base;
using UnturnedBlackout.Extensions;

namespace UnturnedBlackout.Database.Data;

public class PlayerLoadout : IDisposable
{
    public CSteamID SteamID { get; set; }
    public Dictionary<ushort, LoadoutGun> Guns { get; set; }
    public Dictionary<ushort, LoadoutGunCharm> GunCharms { get; set; }
    public Dictionary<ushort, LoadoutKnife> Knives { get; set; }
    public Dictionary<int, GunSkin> GunSkinsSearchByID { get; set; }
    public Dictionary<ushort, List<GunSkin>> GunSkinsSearchByGunID { get; set; }
    public Dictionary<ushort, GunSkin> GunSkinsSearchBySkinID { get; set; }
    public Dictionary<int, LoadoutPerk> Perks { get; set; }
    public Dictionary<ushort, LoadoutGadget> Gadgets { get; set; }
    public Dictionary<int, LoadoutKillstreak> Killstreaks { get; set; }
    public Dictionary<int, LoadoutDeathstreak> Deathstreaks { get; set; }
    public Dictionary<int, LoadoutAbility> Abilities { get; set; }
    public Dictionary<int, LoadoutCard> Cards { get; set; }
    public Dictionary<int, LoadoutGlove> Gloves { get; set; }
    public Dictionary<int, Loadout> Loadouts { get; set; }

    public PlayerLoadout(CSteamID steamID, Dictionary<ushort, LoadoutGun> guns, Dictionary<ushort, LoadoutGunCharm> gunCharms, Dictionary<ushort, LoadoutKnife> knives, Dictionary<int, GunSkin> gunSkinsSearchByID, Dictionary<ushort, List<GunSkin>> gunSkinsSearchByGunID, Dictionary<ushort, GunSkin> gunSkinsSearchBySkinID, Dictionary<int, LoadoutPerk> perks, Dictionary<ushort, LoadoutGadget> gadgets, Dictionary<int, LoadoutKillstreak> killstreaks, Dictionary<int, LoadoutDeathstreak> deathstreaks, Dictionary<int, LoadoutAbility> abilities, Dictionary<int, LoadoutCard> cards, Dictionary<int, LoadoutGlove> gloves, Dictionary<int, Loadout> loadouts)
    {
        SteamID = steamID;
        Guns = guns;
        GunCharms = gunCharms;
        Knives = knives;
        GunSkinsSearchByID = gunSkinsSearchByID;
        GunSkinsSearchByGunID = gunSkinsSearchByGunID;
        GunSkinsSearchBySkinID = gunSkinsSearchBySkinID;
        Perks = perks;
        Gadgets = gadgets;
        Killstreaks = killstreaks;
        Deathstreaks = deathstreaks;
        Abilities = abilities;
        Cards = cards;
        Gloves = gloves;
        Loadouts = loadouts;
    }

    public void Dispose()
    {
        Logging.Debug($"PlayerLoadout is being disposed/finalised. Generation: {GC.GetGeneration(this)}", ConsoleColor.Blue);
        Guns = null;
        GunCharms = null;
        Knives = null;
        GunSkinsSearchByID = null;
        GunSkinsSearchByGunID = null;
        GunSkinsSearchBySkinID = null;
        Perks = null;
        Gadgets = null;
        Killstreaks = null;
        Deathstreaks = null;
        Abilities = null;
        Cards = null;
        Gloves = null;
        Loadouts = null;
    }

    ~PlayerLoadout()
    {
        Logging.Debug($"PlayerLoadout is being destroyed/finalised", ConsoleColor.Magenta);
    }
}