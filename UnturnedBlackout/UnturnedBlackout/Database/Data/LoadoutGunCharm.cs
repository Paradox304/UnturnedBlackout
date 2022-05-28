using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Database.Data
{
    public class LoadoutGunCharm
    {
        public GunCharm GunCharm { get; set; }
        public bool IsBought { get; set; }

        public LoadoutGunCharm(GunCharm gunCharm, bool isBought)
        {
            GunCharm = gunCharm;
            IsBought = isBought;
        }
    }
}
