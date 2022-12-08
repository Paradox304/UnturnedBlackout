using System;

namespace UnturnedBlackout.Models.Global;

[Serializable]
public class CustomGun
{
    public ushort GunID { get; set; }
    public ushort SightsID { get; set; }
    public ushort GripID { get; set; }
    public ushort BarrelID { get; set; }
    public ushort MagID { get; set; }

    public CustomGun()
    {
    }
}