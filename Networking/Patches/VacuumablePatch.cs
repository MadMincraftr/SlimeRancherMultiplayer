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
    [HarmonyPatch(typeof(Vacuumable), nameof(Vacuumable.capture))]
    public class VacuumableCapture
    {
        public static void Postfix(Vacuumable __instance, Joint toJoint)
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                var actor = __instance.GetComponent<NetworkActorOwnerToggle>();
                if (actor != null)
                {
                    actor.OwnActor();
                }
            }
        }
    }
    [HarmonyPatch(typeof(Vacuumable), nameof(Vacuumable.TryConsume))]
    public class VacuumableTryConsume
    {
        public static bool Prefix(Vacuumable __instance, bool __result)
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                var ammo = SceneContext.Instance.PlayerState.Ammo;
                if (!(ammo is NetworkAmmo)) return true;

                NetworkAmmo netAmmo = (NetworkAmmo)ammo;
                var openSlot = netAmmo.GetSlotIDX(__instance.identifiable.id);
                if (openSlot == -1)
                {
                    __result = false;
                    return false;
                }
                netAmmo.MaybeAddToSpecificSlot(__instance.identifiable.id, __instance.identifiable, openSlot);
                Destroyer.Destroy(__instance.gameObject, "SRMP.NetworkVac");
                __result = true;
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Vacuumable), nameof(Vacuumable.SetHeld))]
    public class VacuumableSetHeld
    {
        public static void Prefix(Vacuumable __instance, bool held)
        {
            if (!held) return;

            if (NetworkServer.active || NetworkClient.active)
            {
                var actor = __instance.GetComponent<NetworkActorOwnerToggle>();
                if (actor != null)
                {
                    actor.OwnActor();
                }
            }
        }
    }
}
