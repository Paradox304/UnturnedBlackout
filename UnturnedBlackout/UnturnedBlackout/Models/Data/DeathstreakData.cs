using System;

namespace UnturnedBlackout.Models.Data;

[Serializable]
public class DeathstreakData
{
    public int DeathstreakID { get; set; }
    public ushort TriggerItemID { get; set; }

    public bool IsItem { get; set; }
    public ushort ItemID { get; set; }
    public bool RemoveWhenAmmoEmpty { get; set; }
    public ushort MagID { get; set; }
    public int MagAmount { get; set; }
    public string MedalName { get; set; }
    public int MedalXP { get; set; }

    public bool IsClothing { get; set; }

    public bool HasInfiniteStamina { get; set; }
    public int DeathstreakStaySeconds { get; set; }

    public string DeathstreakHUDIconURL { get; set; }
}