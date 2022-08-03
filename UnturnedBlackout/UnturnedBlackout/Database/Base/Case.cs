using System.Collections.Generic;
using UnturnedBlackout.Enums;

namespace UnturnedBlackout.Database.Base
{
    public class Case
    {
        public int CaseID { get; set; }
        public string CaseName { get; set; }
        public string IconLink { get; set; }
        public List<(ECaseRarity, int)> Weights { get; set; }
        public List<GunSkin> AvailableSkins { get; set; }

        public Case(int caseID, string caseName, string iconLink, List<(ECaseRarity, int)> weights, List<GunSkin> availableSkins)
        {
            CaseID = caseID;
            CaseName = caseName;
            IconLink = iconLink;
            Weights = weights;
            AvailableSkins = availableSkins;
        }
    }
}
