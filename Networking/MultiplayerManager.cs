﻿using kcp2k;
using Mirror;
using Mirror.Discovery;
using SRMP.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMP.Networking
{
    [DisallowMultipleComponent]
    public class MultiplayerManager : SRSingleton<MultiplayerManager>
    {
        private NetworkManager networkManager;

        public NetworkingMainMenuUI networkMainMenuHUD;

        public NetworkingClientUI networkConnectedHUD;

        public CustomDiscoveryUI networkDiscoverHUD;

        private NetworkDiscovery discoveryManager;

        private GameObject onlinePlayerPrefab;

        public KcpTransport transport;

        public static NetworkManager NetworkManager
        {
            get
            {
                return Instance.networkManager;
            }
        }

        public static NetworkDiscovery DiscoveryManager
        {
            get
            {
                return Instance.discoveryManager;
            }
        }


        public void Awake()
        {
            base.Awake();
        }


        private void Start()
        {
            transport = gameObject.AddComponent<KcpTransport>();
            
            WriterBugfix.FixWriters();
            ReaderBugfix.FixReaders();

            onlinePlayerPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule); // Prototype player.
            onlinePlayerPrefab.AddComponent<NetworkPlayerOnline>();
            onlinePlayerPrefab.AddComponent<NetworkIdentity>();
            onlinePlayerPrefab.AddComponent<NetworkTransformReliable>();
            onlinePlayerPrefab.DontDestroyOnLoad();
            onlinePlayerPrefab.SetActive(false);

            networkManager = gameObject.AddComponent<SRNetworkManager>();

            networkManager.maxConnections = SRMLConfig.MAX_PLAYERS;
            // networkManager.playerPrefab = onlinePlayerPrefab; need to use asset bundles to fix error
            networkManager.autoCreatePlayer = false;

            // EXPERIMENTAL OPTION!
            if (SRMLConfig.EXPERIMENTAL)
            {
                networkManager.offlineScene = "MainMenu";
            }

            networkManager.transport = transport;
            Transport.active = transport;

            networkMainMenuHUD = gameObject.AddComponent<NetworkingMainMenuUI>();
            networkConnectedHUD = gameObject.AddComponent<NetworkingClientUI>();


            discoveryManager = gameObject.AddComponent<NetworkDiscovery>();
            networkDiscoverHUD = gameObject.AddComponent<CustomDiscoveryUI>();

            networkMainMenuHUD.offsetY = Screen.height - 75;
            
            NetworkManager.dontDestroyOnLoad = true;
        }

        public void Connect(string ip, ushort port)
        {
            transport.port = port;
            NetworkClient.Connect(ip);
        }
        public void Host()
        {
            networkManager.StartHost();
            discoveryManager.AdvertiseServer();
            transport.ServerStart();
        }

        private void Update()
        {
            networkMainMenuHUD.enabled = false;
            networkConnectedHUD.enabled = false;
            networkDiscoverHUD.enabled = false;
            if (NetworkServer.active)
            {
                // Show host ui
            }
            else if (NetworkClient.isConnected)
            {
                networkConnectedHUD.enabled = true; // Show connected ui
            }
            else if (NetworkClient.isConnecting)
            {
                // Show connecting ui
            }
            else if (Levels.isMainMenu())
            {
                networkMainMenuHUD.enabled = true; // Show connect ui
                networkDiscoverHUD.enabled = true; // Show connect to lan ui
            }
            else
            {
                // Show no ui
            }

            // TIcks
            if (NetworkServer.activeHost)
            {
                transport.ServerLateUpdate();
                transport.ServerEarlyUpdate();
            }
            else if (NetworkClient.isConnected || NetworkClient.isConnecting)
            {
                transport.ClientEarlyUpdate();
                transport.ClientLateUpdate();
            }
        }
        public delegate void SRMPRegisterHandlerCallbackC(bool hostmode);
        public delegate void SRMPRegisterHandlerCallbackS();

    }
}