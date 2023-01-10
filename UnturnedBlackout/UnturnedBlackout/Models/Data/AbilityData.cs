using System;

namespace UnturnedBlackout.Models.Data;

[Serializable]
public class AbilityData
{
    public int AbilityID { get; set; }
    public ushort TriggerItemID { get; set; }

    public int CooldownSeconds { get; set; }
    public bool IsItem { get; set; }
    public ushort ItemID { get; set; }
    public bool RemoveWhenAmmoEmpty { get; set; }
    public ushort MagID { get; set; }
    public int MagAmount { get; set; }
    public string MedalName { get; set; }
    public int MedalXP { get; set; }
    
    public int AbilityStaySeconds { get; set; }
    public string AbilityHUDIconURL { get; set; }
}