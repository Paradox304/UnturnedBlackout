namespace UnturnedBlackout.Enums;

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
    WinKills, // Amount of kills at the time of match win/finish [Win, FinishMatch]
    WinTags, // Amount of dogtags collected at the time of match win
    WinFlagsCaptured, // Amount of flags captured at the time of match win
    WinFlagsSaved, // Amount of flags saved at the time of match win
    Special // FOR SPECIAL TYPE OF QUESTS [DO NOT SET THIS ON YOUR OWN, ASK ME BEFOREHAND]
}
