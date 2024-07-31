﻿using Mirror;

namespace SRMP.Networking.Packet
{
    public struct LandPlotMessage : NetworkMessage
    {
        public string id;
        public LandPlot.Id type;
        public LandPlot.Upgrade upgrade;
        public LandplotUpdateType messageType;
    }

    public enum LandplotUpdateType : byte
    {
        SET,
        UPGRADE
    }
}
