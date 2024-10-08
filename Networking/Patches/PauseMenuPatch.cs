﻿using HarmonyLib;
using Mirror;
using SRMP.Networking;
using SRMP.Networking.Packet;

namespace SRMP.Networking.Patches
{
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.Quit))]
    internal class PauseMenuQuit
    {
        public static void Postfix(PauseMenu __instance)
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                NetworkServer.Shutdown();
                NetworkClient.Shutdown();

                SRNetworkManager.EraseValues();
            }
        }
    }
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.Start))]
    internal class PauseMenuStart
    {
        public static void Postfix(PauseMenu __instance)
        {
            
            if (NetworkClient.isConnected) SRMP.ReplaceTranslation("ui", "Disconnect", "b.save_and_quit");
        }
    }
}
