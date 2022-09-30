namespace UnturnedBlackout.Enums;

public enum EQuestCondition
{
    GUN, // Gun you are using
    GUN_TYPE, // Gun type you are using
    GADGET, // Gadget you are using
    KNIFE, // Knife you are using
    KILLSTREAK, // Killstreak you are using [TURRET/RPG/AGM/CHOPPER GUNNER ETC]
    MAP, // Map you are playing on
    GAMEMODE, // Gamemode you are playing on
    TARGET_MK, // Amount of multi kill you need to get
    TARGET_KS, // Amount of killstreak you need to get
    WIN_KILLS, // Amount of kills at the time of match win/finish [Win, FinishMatch]
    WIN_TAGS, // Amount of dogtags collected at the time of match win
    WIN_FLAGS_CAPTURED, // Amount of flags captured at the time of match win
    WIN_FLAGS_SAVED, // Amount of flags saved at the time of match win
    SPECIAL // FOR SPECIAL TYPE OF QUESTS [DO NOT SET THIS ON YOUR OWN, ASK ME BEFOREHAND]
}