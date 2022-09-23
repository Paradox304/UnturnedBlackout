namespace UnturnedBlackout.Models.Data
{
    public class KillstreakData
    {
        public int KillstreakID { get; set; }
        public ushort TriggerItemID { get; set; }
        public bool IsItem { get; set; }
        public ushort ItemID { get; set; }
        public bool RemoveWhenAmmoEmpty { get; set; }
        public int ItemStaySeconds { get; set; }
        public ushort MagID { get; set; }
        public int MagAmount { get; set; }
        public string MedalName { get; set; }
        public int MedalXP { get; set; }
        public float MovementMultiplier { get; set; }

        public KillstreakData()
        {

        }
    }
}
