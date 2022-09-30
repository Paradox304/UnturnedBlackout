namespace UnturnedBlackout.Enums;

public enum EQuestType
{
    KILL, // Kill a certain amount of players
    DEATH, // Die a certain amount of times
    WIN, // Win a certain amount of games
    FINISH_MATCH, // Finish a match
    MULTI_KILL, // Kill a certain amount of players in a row
    KILLSTREAK, // Kill a certain amount of players before dying
    HEADSHOTS, // Hit a player on the skull
    GADGETS_USED, // Use a certain amount of gadgets
    FLAGS_CAPTURED, // Capture a certain amount of flags
    FLAGS_SAVED, // Save a certain amount of flags
    DOGTAGS, // Collect a certain amount of dogtags
    SHUTDOWN, // Shutdown players
    DOMINATION, // Dominate players
    FLAG_KILLER, // Kill flag carriers
    FLAG_DENIED, // Kill people while carrying a flag
    REVENGE, // Kill the player after being killed by them
    FIRST_KILL, // Get the first kill of the match
    LONGSHOT, // Get a kill from more than X meters
    SURVIVOR, // Get a kill with health below X
    COLLECTOR // Collect X tags in a row
}