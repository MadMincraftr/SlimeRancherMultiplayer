﻿using HarmonyLib;
using Mirror;

namespace SRMP.Networking.Patches
{
    [HarmonyPatch(typeof(SpawnResource), nameof(SpawnResource.Awake))]
    internal class SpawnResourceAwake
    {
        public static void Postfix(SpawnResource __instance)
        {
            if (NetworkClient.active && !NetworkServer.activeHost)
                __instance.model.nextSpawnTime = double.MaxValue;
        }
    }
}
