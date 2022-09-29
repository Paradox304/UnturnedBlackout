namespace UnturnedBlackout.Models.Data;

public class KillstreakData
{
    public int KillstreakID { get; set; }
    public ushort TriggerItemID { get; set; }

    public bool IsItem { get; set; }
    public ushort ItemID { get; set; }
    public bool RemoveWhenAmmoEmpty { get; set; }
    public ushort MagID { get; set; }
    public int MagAmount { get; set; }
    public string MedalName { get; set; }
    public int MedalXP { get; set; }

    public bool IsClothing { get; set; }

    public bool IsTurret { get; set; }
    public ushort TurretID { get; set; }
    public ushort GunID { get; set; }
    public int TurretDamagePerSecond { get; set; }

    public bool HasInfiniteStamina { get; set; }
    public int KillstreakStaySeconds { get; set; }
    public float MovementMultiplier { get; set; }

    public KillstreakData()
    {
    }
}