﻿using Mirror;
using Mirror.Discovery;
using SRMP.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMP.Networking
{
    public class ReaderBugfix
    {
        public static void FixReaders()
        {
            Reader<ServerRequest>.read = new Func<NetworkReader, ServerRequest>((r) => NetworkReaderExtensions.ReadDiscoveryRequestMessage(r)); 
            Reader<ServerResponse>.read = new Func<NetworkReader, ServerResponse>((r) => NetworkReaderExtensions.ReadDiscoveryResponseMessage(r));
            Reader<TestLogMessage>.read = new Func<NetworkReader, TestLogMessage>((r) => NetworkReaderExtensions.ReadTestLogMessage(r));
            Reader<NetworkPingMessage>.read = new Func<NetworkReader, NetworkPingMessage>((r) => NetworkReaderExtensions.ReadPingMessage(r));
            Reader<SceneMessage>.read = new Func<NetworkReader, SceneMessage>((r) => NetworkReaderExtensions.ReadSceneMessage(r));
            Reader<ReadyMessage>.read = new Func<NetworkReader, ReadyMessage>((r) => NetworkReaderExtensions.ReadReadyMessage(r));
            Reader<NotReadyMessage>.read = new Func<NetworkReader, NotReadyMessage>((r) => NetworkReaderExtensions.ReadUnreadyMessage(r));
            Reader<TimeSnapshotMessage>.read = new Func<NetworkReader, TimeSnapshotMessage>((r) => NetworkReaderExtensions.ReadTimeSnapshotMessage(r));
            Reader<AddPlayerMessage>.read = new Func<NetworkReader, AddPlayerMessage>((r) => NetworkReaderExtensions.ReadAddPlayerMessage(r));
            Reader<SetMoneyMessage>.read = new Func<NetworkReader, SetMoneyMessage>((r) => NetworkReaderExtensions.ReadMoneyMessage(r));
            Reader<PlayerUpdateMessage>.read = new Func<NetworkReader, PlayerUpdateMessage>((r) => NetworkReaderExtensions.ReadPlayerMessage(r));
            Reader<PlayerJoinMessage>.read = new Func<NetworkReader, PlayerJoinMessage>((r) => NetworkReaderExtensions.ReadPlayerJoinMessage(r));
            Reader<TimeSyncMessage>.read = new Func<NetworkReader, TimeSyncMessage>((r) => NetworkReaderExtensions.ReadTimeMessage(r));
            Reader<SleepMessage>.read = new Func<NetworkReader, SleepMessage>((r) => NetworkReaderExtensions.ReadSleepMessage(r));
            Reader<ActorSpawnClientMessage>.read = new Func<NetworkReader, ActorSpawnClientMessage>((r) => NetworkReaderExtensions.ReadActorSpawnClientMessage(r));
            Reader<ActorSpawnMessage>.read = new Func<NetworkReader, ActorSpawnMessage>((r) => NetworkReaderExtensions.ReadActorSpawnMessage(r));
            Reader<ActorUpdateClientMessage>.read = new Func<NetworkReader, ActorUpdateClientMessage>((r) => NetworkReaderExtensions.ReadActorClientMessage(r));
            Reader<ActorUpdateMessage>.read = new Func<NetworkReader, ActorUpdateMessage>((r) => NetworkReaderExtensions.ReadActorMessage(r));
            Reader<ActorUpdateOwnerMessage>.read = new Func<NetworkReader, ActorUpdateOwnerMessage>((r) => NetworkReaderExtensions.ReadActorOwnMessage(r));
            Reader<ActorDestroyGlobalMessage>.read = new Func<NetworkReader, ActorDestroyGlobalMessage>((r) => NetworkReaderExtensions.ReadActorDestroyMessage(r));
            Reader<LandPlotMessage>.read = new Func<NetworkReader, LandPlotMessage>((r) => NetworkReaderExtensions.ReadLandPlotMessage(r));
            Reader<GordoBurstMessage>.read = new Func<NetworkReader, GordoBurstMessage>((r) => NetworkReaderExtensions.ReadGordoBurstMessage(r));
            Reader<GordoEatMessage>.read = new Func<NetworkReader, GordoEatMessage>((r) => NetworkReaderExtensions.ReadGordoEatMessage(r));
        }
    }
}
