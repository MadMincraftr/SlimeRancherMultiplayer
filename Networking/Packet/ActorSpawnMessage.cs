﻿using Mirror;
using MonomiPark.SlimeRancher.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMP.Networking.Packet
{
    public struct ActorSpawnMessage : NetworkMessage
    {
        public long id;
        public Vector3 position;
        public Vector3 rotation;
        public Identifiable.Id ident;
        public RegionRegistry.RegionSetId region;
        public int player;
    }
}
