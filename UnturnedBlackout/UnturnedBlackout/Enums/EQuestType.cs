namespace UnturnedBlackout.Enums
{
    public enum EQuestType
    {
        Kill, // Kill a certain amount of players
        Death, // Die a certain amount of times
        Win, // Win a certain amount of games
        FinishMatch, // Finish a match
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
        FlagDenied, // Kill people while carrying a flag
        Revenge, // Kill the player after being killed by them
        FirstKill, // Get the first kill of the match
        Longshot, // Get a kill from more than X meters
        Survivor, // Get a kill with health below X
        Collector, // Collect X tags in a row
    }
}
