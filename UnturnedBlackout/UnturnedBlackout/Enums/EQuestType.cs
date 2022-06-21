namespace UnturnedBlackout.Enums
{
    public enum EQuestType
    {
        Kill, // Kill a certain amount of players
        Death, // Die a certain amount of times
        Win, // Win a certain amount of games
        MultiKill, // Kill a certain amount of players in a row
        Killstreak, // Kill a certain amount of players before dying
        Headshots, // Hit a player on the skull
        GadgetsUsed, // Use a certain amount of gadgets
        FlagsCaptured, // Capture a certain amount of flags
        FlagsSaved, // Save a certain amount of flags
        Dogtags, // Collect a certain amount of dogtags
        Shutdown, // Shutdown players
        Domination, // Dominate players
        FlagKiller, // Kill flag carriers
        Revenge, // Kill the player after being killed by them
    }
}
