﻿using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace UnturnedLegends.Commands
{
    class LeaveCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "leave";

        public string Help => "Leave the game going on";

        public string Syntax => "/leave";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            Plugin.Instance.GameManager.RemovePlayerFromGame(player);
        }
    }
}