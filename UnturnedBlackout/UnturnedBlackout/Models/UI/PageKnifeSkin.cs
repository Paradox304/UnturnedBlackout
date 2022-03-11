﻿using System.Collections.Generic;
using UnturnedBlackout.Database.Base;

namespace UnturnedBlackout.Models.UI
{
    public class PageKnifeSkin
    {
        public int PageID { get; set; }
        public Dictionary<int, KnifeSkin> KnifeSkins { get; set; }

        public PageKnifeSkin(int pageID, Dictionary<int, KnifeSkin> knifeSkins)
        {
            PageID = pageID;
            KnifeSkins = knifeSkins;
        }
    }
}