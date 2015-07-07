﻿
using tdsm.api.Plugin;
using Terraria;
using System;

namespace tdsm.api.Callbacks
{
    public static class VanillaHooks
    {
        public static void OnPlayerEntering(Player player)
        {
            var ctx = new HookContext
            {
                Player = player,
                Sender = player
            };

            var args = new HookArgs.PlayerEnteringGame
            {
                Slot = player.whoAmI
            };

            ctx.SetResult(HookResult.DEFAULT, false);
            HookPoints.PlayerEnteringGame.Invoke(ref ctx, ref args);
            ctx.CheckForKick();
        }

        public static bool OnGreetPlayer(int playerId)
        {
            var player = Main.player[playerId];

            var ctx = new HookContext
            {
                Connection = player.Connection.Socket,
                Player = player,
                Sender = player,
            };

            var args = new HookArgs.PlayerPreGreeting
            {
                Slot = playerId,
                Motd = String.IsNullOrEmpty(Main.motd) ? Lang.mp[18] : Main.motd,
                MotdColour = new Microsoft.Xna.Framework.Color(255, 240, 20)
            };

            HookPoints.PlayerPreGreeting.Invoke(ref ctx, ref args);

            if (ctx.CheckForKick())
            {
                return false;
            }

            player.SendMessage(args.Motd, 255, args.MotdColour.R, args.MotdColour.G, args.MotdColour.B);

            string list = "";
            for (int i = 0; i < 255; i++)
            {
                if (Main.player[i].active)
                {
                    if (list == "")
                        list += Main.player[i].name;
                    else
                        list = list + ", " + Main.player[i].Name;
                }
            }

            player.SendMessage("Current players: " + list + ".", 255, 255, 240, 20);

            Tools.WriteLine("{0} @ {1}: ENTER {2}", Netplay.Clients[playerId].Socket.GetRemoteAddress(), playerId, player.name);

            var args2 = new HookArgs.PlayerEnteredGame
            {
                Slot = playerId
            };

            ctx.SetResult(HookResult.DEFAULT, false);
            HookPoints.PlayerEnteredGame.Invoke(ref ctx, ref args2);
            ctx.CheckForKick();

            return false; //We implemented our own, so do not continue on with vanilla
        }
    }
}