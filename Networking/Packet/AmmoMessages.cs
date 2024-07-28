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
    public struct AmmoEditSlotMessage : NetworkMessage
    {
        public Identifiable.Id ident;
        public int slot;
        public int count;
        public string id;
    }
    public struct AmmoAddMessage : NetworkMessage
    {
        public Identifiable.Id ident;
        public string id;
    }
    public struct AmmoRemoveMessage : NetworkMessage
    {
        public int index;
        public int count;
        public string id;
    }
}
