﻿using kcp2k;
using Mirror;
using Mirror.Discovery;
using MonomiPark.SlimeRancher.Persist;
using SRMP.Networking.Component;
using SRMP.Networking.Packet;
using SRMP.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using SRMP.Networking.UI;

namespace SRMP.Networking
{
    [DisallowMultipleComponent]
    public class MultiplayerManager : SRBehaviour
    {
        internal static AssetBundle uiBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("SRMP.ui"));


        private NetworkManager networkManager;

        public NetworkingIngameUI networkInGameHUD;

        public NetworkingMainMenuUI networkMainMenuHUD;

        public NetworkingClientUI networkConnectedHUD;

        public CustomDiscoveryUI networkDiscoverHUD;

        private NetworkDiscovery discoveryManager;

        public GameObject onlinePlayerPrefab;

        public KcpTransport transport;

        GUIStyle guiStyle;

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

        public static MultiplayerManager Instance;

        public void Awake()
        {
            Instance = this;
        }


        private void Start()
        {
            var ui = Instantiate(uiBundle.LoadAllAssets<GameObject>()[0]);
            ui.transform.parent = transform;

            ui.GetChild(4).SetActive(true);

            foreach (var text in ui.transform.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.alignment = TextAlignmentOptions.Center;
            }

            GeneratePlayerBean();

            transport = gameObject.AddComponent<KcpTransport>();
            
            WriterBugfix.FixWriters();
            ReaderBugfix.FixReaders();
            
            networkManager = gameObject.AddComponent<SRNetworkManager>();

            networkManager.maxConnections = SRMLConfig.MAX_PLAYERS;
            // networkManager.playerPrefab = onlinePlayerPrefab; need to use asset bundles to fix error
            networkManager.autoCreatePlayer = false;


            networkManager.transport = transport;
            Transport.active = transport;

            networkMainMenuHUD = gameObject.AddComponent<NetworkingMainMenuUI>();
            
            networkConnectedHUD = gameObject.AddComponent<NetworkingClientUI>();

            networkInGameHUD = gameObject.AddComponent<NetworkingIngameUI>();


            discoveryManager = gameObject.AddComponent<NetworkDiscovery>();
            networkDiscoverHUD = gameObject.AddComponent<CustomDiscoveryUI>();

            networkMainMenuHUD.offsetY = Screen.height - 75;
            
            NetworkManager.dontDestroyOnLoad = true;
            discoveryManager.enableActiveDiscovery = true;



            NetworkClient.OnDisconnectedEvent += ClientLeave;
            networkMainMenuHUD.ui.GetChild(4).GetComponent<TMP_InputField>().text = SRMLConfig.DEFAULT_CONNECT_IP;
            networkMainMenuHUD.ui.GetChild(2).GetComponent<TMP_InputField>().text = SRMLConfig.DEFAULT_PORT.ToString();
        }

        void GeneratePlayerBean()
        {
            onlinePlayerPrefab = new GameObject("PlayerDefault");
            var playerModel = GameObject.CreatePrimitive(PrimitiveType.Capsule); // Prototype player.
            var playerFace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerFace.transform.parent = playerModel.transform;
            playerFace.transform.localPosition = new Vector3(0f, 0.5f, 0.25f);
            playerFace.transform.localScale = Vector3.one * 0.5f;
            onlinePlayerPrefab.AddComponent<NetworkPlayer>();
            onlinePlayerPrefab.AddComponent<TransformSmoother>();
            onlinePlayerPrefab.GetComponent<NetworkPlayer>().enabled = false;
            onlinePlayerPrefab.DontDestroyOnLoad();
            onlinePlayerPrefab.SetActive(false);
            playerModel.transform.parent = onlinePlayerPrefab.transform;

            var material = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault((mat) => mat.name == "slimePinkBase");
            playerFace.GetComponent<MeshRenderer>().material = material;
            playerModel.GetComponent<MeshRenderer>().material = material;

            Destroy(playerFace.GetComponent<BoxCollider>());

            playerModel.transform.localPosition = Vector3.up;
        }

        public void OnDestroy()
        {
            SRMP.Log("THIS SHOULD NOT APPEAR!!!!");
        }

        // Hefty code
        public static void PlayerJoin(NetworkConnectionToClient nctc)
        {
            SRMP.Log("connecting client.");
            List<InitActorData> actors = new List<InitActorData>();
            List<InitPlayerData> players = new List<InitPlayerData>();
            List<InitPlotData> plots = new List<InitPlotData>();
            HashSet<PediaDirector.Id> pedias = new HashSet<PediaDirector.Id>();
            foreach (var a in Resources.FindObjectsOfTypeAll<Identifiable>())
            {
                try
                {

                    if (a.gameObject.scene.name == "worldGenerated")
                    {
                        var data = new InitActorData()
                        {
                            id = a.GetActorId(),
                            ident = a.id,
                            pos = a.transform.position
                        };
                        actors.Add(data);
                    }
                }
                catch { }
            }

            foreach (var player in SRNetworkManager.players)
            {
                if (player.Key != 0) // idk how my code works anymore and too lazy to try catch. // Note, quite the opposite: not lazy enough to try catch. :skull:
                {

                    var p = new InitPlayerData()
                    {
                        id = player.Key,
                    };
                    players.Add(p);
                }
            }
            foreach (var plot in Resources.FindObjectsOfTypeAll<LandPlot>())
            {
                if (plot.gameObject.scene.name == "worldGenerated") // Dunno if there are plots in hide and dont save...
                {
                    try
                    {
                        var p = new InitPlotData()
                        {
                            id = plot.model.gameObj.GetComponent<LandPlotLocation>().id,
                            type = plot.model.typeId,
                            upgrades = plot.model.upgrades,
                        };
                        plots.Add(p);
                    }
                    catch { }
                }
            }
            pedias = SceneContext.Instance.PediaDirector.pediaModel.unlocked;
            var p2 = new InitPlayerData()
            {
                id = 0
            };
            players.Add(p2);
            var saveMessage = new LoadMessage()
            {
                initActors = actors,
                initPlayers = players,
                initPlots = plots,
                initPedias = pedias,
                playerID = nctc.connectionId,
                money = SceneContext.Instance.PlayerState.model.currency
            };
            NetworkServer.SRMPSend(saveMessage, nctc);
            SRMP.Log("sent world");

            try
            {
                var player = Instantiate(Instance.onlinePlayerPrefab);
                player.name = $"Player{nctc.connectionId}";
                var netPlayer = player.GetComponent<NetworkPlayer>();
                SRNetworkManager.players.Add(nctc.connectionId, netPlayer);
                netPlayer.id = nctc.connectionId;
                player.SetActive(true);
                var packet = new PlayerJoinMessage()
                {
                    id = nctc.connectionId,
                    local = false
                };
                NetworkServer.SRMPSendToConnections(packet, NetworkServer.NetworkConnectionListExcept(nctc));
            }
            catch 
            { }
            
        }

        public static void ClientLeave()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void Connect(string ip, ushort port)
        {
            networkManager.OnStartClient();
            transport.port = port;
            NetworkClient.Connect(ip);
        }
        public bool isHosting;
        public void Host(ushort port)
        {
            transport.port = port;
            networkManager.StartHost();
            transport.ServerStart();
            discoveryManager.AdvertiseServer();
            isHosting = true;
        }

        private void Update()
        {
            if (NetworkServer.active)
            {
                networkConnectedHUD.enabled = false;
                networkMainMenuHUD.enabled = false;
                networkDiscoverHUD.enabled = false;
                // Show host ui
            }
            else if (NetworkClient.isConnected)
            {
                networkMainMenuHUD.enabled = false;
                networkInGameHUD.enabled = false;
                networkDiscoverHUD.enabled = false;
                networkConnectedHUD.enabled = true; // Show connected ui
            }
            else if (NetworkClient.isConnecting)
            {
                networkConnectedHUD.enabled = false;
                networkMainMenuHUD.enabled = false;
                networkInGameHUD.enabled = false;
                networkDiscoverHUD.enabled = false;
                // Show connecting ui
            }
            else if (Levels.isMainMenu())
            {
                networkConnectedHUD.enabled = false;
                networkInGameHUD.enabled = false;
                networkMainMenuHUD.enabled = true; // Show connect ui
                networkDiscoverHUD.enabled = true; // Show connect to lan ui
            }
            else if (Time.timeScale == 0 && !NetworkClient.isConnected && !NetworkClient.isConnecting && !NetworkServer.activeHost)
            {
                networkConnectedHUD.enabled = false;
                networkMainMenuHUD.enabled = false;
                networkInGameHUD.enabled = true; // Show ingame ui
                networkDiscoverHUD.enabled = false;
            }
            else
            {
                // Show no ui
                networkConnectedHUD.enabled = false;
                networkMainMenuHUD.enabled = false;
                networkInGameHUD.enabled = false;
                networkDiscoverHUD.enabled = false;
            }

            // TIcks
            if (NetworkServer.activeHost)
            {
                transport.ServerEarlyUpdate();
                transport.ServerLateUpdate();
            }
            else if (NetworkClient.active || NetworkClient.isConnecting)
            {
                transport.ClientEarlyUpdate();
                transport.ClientLateUpdate();
            }
        }
    }
}
