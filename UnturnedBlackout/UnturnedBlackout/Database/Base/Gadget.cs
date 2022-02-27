﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.Database.Base
{
    public class Gadget
    {
        public ushort GadgetID { get; set; }
        public string GadgetName { get; set; }
        public string GadgetDesc { get; set; }
        public string IconLink { get; set; }
        public int ScrapAmount { get; set; }
        public int BuyPrice { get; set; }
        public int GiveSeconds { get; set; }
        public bool IsDefault { get; set; }

        public Gadget(ushort gadgetID, string gadgetName, string gadgetDesc, string iconLink, int scrapAmount, int buyPrice, int giveSeconds, bool isDefault)
        {
            GadgetID = gadgetID;
            GadgetName = gadgetName;
            GadgetDesc = gadgetDesc;
            IconLink = iconLink;
            ScrapAmount = scrapAmount;
            BuyPrice = buyPrice;
            GiveSeconds = giveSeconds;
            IsDefault = isDefault;
        }
    }
}
