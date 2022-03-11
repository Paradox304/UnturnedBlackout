namespace UnturnedBlackout.Models.Global
{
    public class LoadoutAmount
    {
        public string Permission { get; set; }
        public int Amount { get; set; }

        public LoadoutAmount(string permission, int amount)
        {
            Permission = permission;
            Amount = amount;
        }

        public LoadoutAmount()
        {

        }
    }
}
