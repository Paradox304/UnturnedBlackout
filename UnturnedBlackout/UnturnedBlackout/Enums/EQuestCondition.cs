using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Enums
{
    public enum EQuestCondition
    {
        Gun, // Gun you are using
        GunType, // Gun type you are using
        Gadget, // Gadget you are using
        Knife, // Knife you are using
        Killstreak, // Killstreak you are using [TURRET/RPG/AGM/CHOPPER GUNNER ETC]
        Map, // Map you are playing on
        Gamemode, // Gamemode you are playing on
        TargetMK, // Amount of multi kill you need to get
        TargetKS, // Amount of killstreak you need to get
        Special // FOR SPECIAL TYPE OF QUESTS [DO NOT SET THIS ON YOUR OWN, ASK ME BEFOREHAND]
    }
}
