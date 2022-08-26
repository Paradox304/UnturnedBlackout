using System.Collections.Generic;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class Case
    {
        public int CaseID { get; set; }
        public string CaseName { get; set; }
        public string IconLink { get; set; }
        public ERarity CaseRarity { get; set; }
        public bool IsBuyable { get; set; }
        public int ScrapPrice { get; set; }
        public int CoinPrice { get; set; }
        public List<(ECaseRarity, int)> Weights { get; set; }
        public Dictionary<ERarity, List<GunSkin>> AvailableSkins { get; set; }

        public Case(int caseID, string caseName, string iconLink, ERarity caseRarity, bool isBuyable, int scrapPrice, int coinPrice, List<(ECaseRarity, int)> weights, Dictionary<ERarity, List<GunSkin>> availableSkins)
        {
            CaseID = caseID;
            CaseName = caseName;
            IconLink = iconLink;
            CaseRarity = caseRarity;
            IsBuyable = isBuyable;
            ScrapPrice = scrapPrice;
            CoinPrice = coinPrice;
            Weights = weights;
            AvailableSkins = availableSkins;
        }
    }
}
