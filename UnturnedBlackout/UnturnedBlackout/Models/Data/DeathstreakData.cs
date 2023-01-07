using System;

namespace UnturnedBlackout.Models.Data;

[Serializable]
public class DeathstreakData
{
    public int DeathstreakID { get; set; }
    
    public float SpeedMultiplier { get; set; }
    public bool InfiniteStamina { get; set; }
    public int DeathstreakStaySeconds { get; set; }

    public string DeathstreakHUDIconURL { get; set; }
}