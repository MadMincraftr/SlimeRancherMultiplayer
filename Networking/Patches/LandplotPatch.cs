﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Mirror;
using MonomiPark.SlimeRancher.DataModel;
using rail;
using SRMP.Networking.Component;
using SRMP.Networking.Packet;
using UnityEngine;
namespace SRMP.Networking.Patches
{
    [HarmonyPatch(typeof(LandPlot), nameof(LandPlot.AddUpgrade))]
    public class LandPlotApplyUpgrades
    {
        public static void Prefix(LandPlot __instance, LandPlot.Upgrade upgrade)
        {
            try
            {
                if ((NetworkServer.active || NetworkClient.active) && __instance.GetComponent<HandledDummy>() == null)
                {
                    var packet = new LandPlotMessage()
                    {
                        id = __instance.model.gameObj.GetComponent<LandPlotLocation>().id,
                        upgrade = upgrade,
                        messageType = LandplotUpdateType.UPGRADE
                    };

                    SRNetworkManager.NetworkSend(packet);
                }
            }
            catch { }
        }

    }
    [HarmonyPatch(typeof(LandPlot), nameof(LandPlot.DestroyAttached))]
    public class LandPlotDestroyAttached
    {
        public static void Postfix(LandPlot __instance)
        {
            try
            {
                if ((NetworkServer.active || NetworkClient.active) && __instance.GetComponent<HandledDummy>() == null)
                {
                    var packet = new GardenPlantMessage()
                    {
                        id = __instance.model.gameObj.GetComponent<LandPlotLocation>().id,
                        ident = Identifiable.Id.NONE,
                        replace = true,
                    };

                    SRNetworkManager.NetworkSend(packet);
                }
            }
            catch { }
        }

    }
}
