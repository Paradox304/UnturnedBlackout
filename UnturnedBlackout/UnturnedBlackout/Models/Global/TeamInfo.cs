﻿using System.Collections.Generic;

namespace UnturnedBlackout.Models.Global
{
    public class TeamInfo
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public string TeamColorHexCode { get; set; }
        public string KillFeedHexCode { get; set; }
        public string ChatPlayerHexCode { get; set; }
        public string ChatMessageHexCode { get; set; }
        public List<TeamKit> TeamKits { get; set; }

        public TeamInfo(int teamID, string teamName, string teamColorHexCode, string killFeedHexCode, string chatPlayerHexCode, string chatMessageHexCode, List<TeamKit> teamKits)
        {
            TeamID = teamID;
            TeamName = teamName;
            TeamColorHexCode = teamColorHexCode;
            KillFeedHexCode = killFeedHexCode;
            ChatPlayerHexCode = chatPlayerHexCode;
            ChatMessageHexCode = chatMessageHexCode;
            TeamKits = teamKits;
        }

        public TeamInfo()
        {

        }
    }
}
