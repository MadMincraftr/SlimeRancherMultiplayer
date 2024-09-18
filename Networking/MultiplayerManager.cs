﻿using kcp2k;
using Mirror;
using Mirror.Discovery;
using MonomiPark.SlimeRancher.Persist;
using SRMP.Networking.Component;
using SRMP.Networking.Packet;
using SRMP.Networking.Patches;
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
using SRMP.Command;
using rail;
using UnityEngine.UI;
using Mirror.FizzySteam;
using SRMP.Networking.Steam;
using SRMP.Networking.SaveModels;
using static PlayerState;

namespace SRMP.Networking
{
    [DisallowMultipleComponent]
    public class MultiplayerManager : SRBehaviour
    {
        /// <summary>
        /// The delegate for the 'OnPlayerConnecting' event.
        /// </summary>
        /// <param name="connection">Connection ID for the player. This should be on its way to the clients when this event is called.</param>
        public delegate void OnPlayerConnect(NetworkConnectionToClient connection);

        /// <summary>
        /// The delegate for the 'OnJoinAttempt' event.
        /// </summary>
        /// <param name="connection">Connection ID for the player. this gets sent to all clients on join.</param>
        /// <param name="errorIfOccurred">If an error occurred, this is what the exception ToString() is.</param>
        public delegate void OnPlayerAttemptedJoin(NetworkConnectionToClient connection, string errorIfOccurred = null);
        
        /// <summary>
        /// The delegate for the 'OnPlayerLeave...' events.
        /// </summary>
        /// <param name="id">The player id of the disconnecting player. This is actually the server side connection id.</param>
        public delegate void OnPlayerLeft(int id);


        /// <summary>
        /// This event occurrs after a player connects into the server, right before the save data is sent over.
        /// The parameters for this event are a client.
        /// </summary>
        public static event OnPlayerConnect OnPlayerConnecting;

        /// <summary>
        /// This event occurrs after a player leaves the server.
        /// The parameters for this event are the player id.
        /// This runs on both client and server.
        /// </summary>
        public static event OnPlayerLeft OnPlayerLeaveCommon;

        /// <summary>
        /// This event occurrs after a player leaves the server.
        /// The parameters for this event are the player id.
        /// This runs only on the client.
        /// </summary>
        public static event OnPlayerLeft OnPlayerLeaveClient;

        /// <summary>
        /// This event occurrs after a player leaves the server.
        /// The parameters for this event are the player id.
        /// This runs only on the server.
        /// </summary>
        public static event OnPlayerLeft OnPlayerLeaveServer;

        /// <summary>
        /// This event occurrs after a player attempts to join, when the code for sending a save errors or finishes.
        /// The parameters for this event are a client and a error if occurred.
        /// </summary>
        public static event OnPlayerAttemptedJoin OnJoinAttempt;

        internal static AssetBundle uiBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("SRMP.ui"));


        private NetworkManager networkManager;

        public NetworkingIngameUI networkInGameHUD;

        public NetworkingMainMenuUI networkMainMenuHUD;

        public NetworkingClientUI networkConnectedHUD;

        public CustomDiscoveryUI networkDiscoverHUD;

        private NetworkDiscovery discoveryManager;

        public GameObject onlinePlayerPrefab;

        public Transport transport;

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

        public static SteamLobby steamLobby;

        public void Awake()
        {
            Instance = this;
        }


        private void Start()
        {
            gameObject.AddComponent<HandledKey>();

            var ui = Instantiate(uiBundle.LoadAllAssets<GameObject>()[0]);
            ui.transform.parent = transform;

            ui.GetChild(4).SetActive(true);


            GeneratePlayerBean();

            /*if (SRMLConfig.USE_PLATFORM)
            {
                SRMP.Log("a");
                transport = gameObject.AddComponent<FizzySteamworks>();
                SRMP.Log("b");
                steamLobby = gameObject.AddComponent<SteamLobby>();
                SRMP.Log("c");
            }
            else */
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


            foreach (var text in ui.transform.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.alignment = TextAlignmentOptions.Center;
            }
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

            var viewcam = new GameObject("CharaCam").AddComponent<Camera>();

            viewcam.transform.parent = playerFace.transform;
            viewcam.transform.localPosition = new Vector3(0, 0, 1.3f);
            viewcam.enabled = false;

            onlinePlayerPrefab.GetComponent<NetworkPlayer>().InitCamera();

        }

        public RenderTexture playerCameraPreviewImage = new RenderTexture(250, 250, 24);

        public NetworkPlayer currentPreviewRenderer;

        public void OnDestroy()
        {
            SRMP.Log("THIS SHOULD NOT APPEAR!!!!");
            SRMP.conInstance.LogError("[SRMP ERROR]");
            SRMP.conInstance.LogError("SRMP has quit unexpectedly, restart your game to play multiplayer.");
        }

        public void AddPreviewToUI()
        {
            var ui = transform.GetChild(0).GetChild(5).GetChild(0).GetChild(0);
            ui.GetComponent<RawImage>().texture = playerCameraPreviewImage;

            ui.parent.parent.GetComponent<Animator>().Play("camRight");
        }
        public void EndPlayerPreview()
        {
            var ui = transform.GetChild(0).GetChild(5);
            currentPreviewRenderer.StopCamera();
            ui.GetComponent<Animator>().Play("camLeft");
        }

        // Hefty code

        public static void PlayerJoin(NetworkConnectionToClient nctc, Guid savingID, string username)
        {
            SRMP.Log("connecting client.");

            OnPlayerConnecting?.Invoke(nctc);



            try
            {
                // Variables
                double time = SceneContext.Instance.TimeDirector.CurrTime();
                List<InitActorData> actors = new List<InitActorData>();
                HashSet<InitGordoData> gordos = new HashSet<InitGordoData>();
                List<InitPlayerData> players = new List<InitPlayerData>();
                List<InitPlotData> plots = new List<InitPlotData>();
                HashSet<PediaDirector.Id> pedias = new HashSet<PediaDirector.Id>();
                List<Upgrade> upgrades = new List<Upgrade>();
                pedias = SceneContext.Instance.PediaDirector.pediaModel.unlocked;

                upgrades = SceneContext.Instance.PlayerState.model.upgrades;


                var newPlayer = !SRNetworkManager.savedGame.savedPlayers.playerList.TryGetValue(savingID, out var playerData);
                if (newPlayer)
                {
                    playerData = new NetPlayerV01();

                    SRNetworkManager.savedGame.savedPlayers.playerList.Add(savingID, playerData);
                }




                // Actors
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

                // Gordos
                foreach (var g in Resources.FindObjectsOfTypeAll<GordoEat>())
                {
                    try
                    {

                        if (g.gameObject.scene.name == "worldGenerated")
                        {
                            InitGordoData data = new InitGordoData()
                            {
                                id = g.id,
                                eaten = g.gordoModel.gordoEatenCount
                            };
                            gordos.Add(data);
                        }
                    }
                    catch { }
                }

                // Current Players
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
                var p2 = new InitPlayerData()
                {
                    id = 0
                };
                players.Add(p2);



                // Plots
                foreach (var plot in Resources.FindObjectsOfTypeAll<LandPlot>())
                {
                    if (plot.gameObject.scene.name == "worldGenerated")
                    {
                        try
                        {
                            var silo = plot.gameObject.GetComponentInChildren<SiloStorage>();

                            // Silos
                            InitSiloData s = new InitSiloData()
                            {
                                ammo = new HashSet<AmmoData>()
                            }; // Empty
                            if (silo != null)
                            {
                                HashSet<AmmoData> ammo = new HashSet<AmmoData>();
                                var idx = 0;
                                foreach (var a in silo.ammo.Slots)
                                {
                                    if (a != null)
                                    {
                                        var ammoSlot = new AmmoData()
                                        {
                                            slot = idx,
                                            id = a.id,
                                            count = a.count,
                                        };
                                        ammo.Add(ammoSlot);
                                    }
                                    else
                                    {
                                        var ammoSlot = new AmmoData()
                                        {
                                            slot = idx,
                                            id = Identifiable.Id.NONE,
                                            count = 0,
                                        };
                                        ammo.Add(ammoSlot);
                                    }
                                    idx++;
                                }
                                s = new InitSiloData()
                                {
                                    slots = silo.numSlots,
                                    ammo = ammo
                                };
                            }

                            var p = new InitPlotData()
                            {
                                id = plot.model.gameObj.GetComponent<LandPlotLocation>().id,
                                type = plot.model.typeId,
                                upgrades = plot.model.upgrades,
                                cropIdent = plot.GetAttachedCropId(),

                                siloData = s,
                            };
                            plots.Add(p);
                        }
                        catch { }
                    }
                }

                // Slime Gates || Ranch expansions
                HashSet<InitAccessData> access = new HashSet<InitAccessData>();
                foreach (var accessDoor in SceneContext.Instance.GameModel.doors)
                {
                    access.Add(new InitAccessData()
                    {
                        open = (accessDoor.Value.state == AccessDoor.State.OPEN),
                        id = accessDoor.Key
                    });
                }
                Dictionary<AmmoMode, List<AmmoData>> playerAmmoData = new Dictionary<AmmoMode, List<AmmoData>>();
                foreach (var ammo in playerData.ammo)
                {
                    List<AmmoData> ammoSlotData = new List<AmmoData>();
                    int i = 0;
                    foreach (var ammoSlot in ammo.Value)
                    {

                        var playerSlot = new AmmoData()
                        {
                            slot = i,
                            id = ammoSlot.id,
                            count = ammoSlot.count,
                        };
                        ammoSlotData.Add(playerSlot);
                        i++;
                    }
                    playerAmmoData.Add(ammo.Key, ammoSlotData);
                }

                LocalPlayerData localPlayerData = new LocalPlayerData()
                {
                    pos = playerData.position.value,
                    rot = playerData.rotation.value,
                    ammo = playerAmmoData
                };


                var keys = SceneContext.Instance.PlayerState.model.keys;
                var money = SceneContext.Instance.PlayerState.model.currency;
                if (!SRNetworkManager.savedGame.sharedKeys)
                    keys = playerData.keys;
                if (!SRNetworkManager.savedGame.sharedMoney)
                    money = playerData.money;
                if (!SRNetworkManager.savedGame.sharedUpgrades)
                    upgrades = playerData.upgrades;


                // Send save data.
                var saveMessage = new LoadMessage()
                {
                    initActors = actors,
                    initPlayers = players,
                    initPlots = plots,
                    initGordos = gordos,
                    initPedias = pedias,
                    initAccess = access,
                    initMaps = SceneContext.Instance.PlayerState.model.unlockedZoneMaps,
                    playerID = nctc.connectionId,
                    money = money,
                    keys = keys,
                    time = time,
                    localPlayerSave = localPlayerData,
                    sharedKeys = SRNetworkManager.savedGame.sharedKeys,
                    sharedMoney = SRNetworkManager.savedGame.sharedMoney,
                    sharedUpgrades = SRNetworkManager.savedGame.sharedUpgrades,
                    upgrades = upgrades,
                };
                NetworkServer.SRMPSend(saveMessage, nctc);
                SRMP.Log("sent world");

                Ammo currentHostAmmoNormal = SceneContext.Instance.PlayerState.GetAmmo(AmmoMode.DEFAULT);
                NetworkAmmo normalNetAmmo = new NetworkAmmo($"player_{savingID}_normal",currentHostAmmoNormal.potentialAmmo,currentHostAmmoNormal.numSlots,currentHostAmmoNormal.ammoModel.usableSlots,currentHostAmmoNormal.slotPreds,currentHostAmmoNormal.ammoModel.slotMaxCountFunction);
                int slotNormalCounter = 0;
                foreach (var ammoSlotNormal in playerAmmoData[AmmoMode.DEFAULT])
                {
                    normalNetAmmo.Slots[slotNormalCounter] = new Ammo.Slot(ammoSlotNormal.id, ammoSlotNormal.count);
                    slotNormalCounter++;
                }

                Ammo currentHostAmmoNimble = SceneContext.Instance.PlayerState.GetAmmo(AmmoMode.NIMBLE_VALLEY);

                NetworkAmmo nimleNetAmmo = new NetworkAmmo($"player_{savingID}_nimble", currentHostAmmoNimble.potentialAmmo, currentHostAmmoNimble.numSlots, currentHostAmmoNimble.ammoModel.usableSlots, currentHostAmmoNimble.slotPreds,currentHostAmmoNimble.ammoModel.slotMaxCountFunction);
                int slotNimbleCounter = 0;
                foreach (var ammoSlotNimble in playerAmmoData[AmmoMode.NIMBLE_VALLEY])
                {
                    nimleNetAmmo.Slots[slotNimbleCounter] = new Ammo.Slot(ammoSlotNimble.id, ammoSlotNimble.count);
                    slotNimbleCounter++;
                }


                // Spawn player for host
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

                    TeleportCommand.playerLookup.Add(TeleportCommand.playerLookup.Count, nctc.connectionId);
                }
                catch
                { }
                OnJoinAttempt?.Invoke(nctc);
                SRNetworkManager.clientToGuid.Add(nctc.connectionId, savingID);
            }
            catch (Exception ex)
            {
                OnJoinAttempt?.Invoke(nctc,ex.ToString());
                SRMP.Log(ex.ToString());
            }
        }


        public static void ClientLeave()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void Connect(string ip, ushort port)
        {

            if (transport is KcpTransport)
                (transport as KcpTransport).port = port;
            networkManager.StartClient();
            NetworkClient.Connect(ip);
        }
        public bool isHosting;
        public void Host(ushort port)
        {
            if (transport is KcpTransport)
            {
                (transport as KcpTransport).port = port;
                networkManager.StartHost();
                transport.ServerStart();
                discoveryManager.AdvertiseServer();
            }
            else if (transport is FizzySteamworks)
            {
                steamLobby.HostLobby();
            }
            isHosting = true;
        }

        private void Update()
        {
            if (NetworkServer.activeHost)
            {
                networkConnectedHUD.enabled = false;
                networkMainMenuHUD.enabled = false;
                networkDiscoverHUD.enabled = false;
            }
            else if (NetworkClient.isConnected)
            {
                networkMainMenuHUD.enabled = false;
                networkInGameHUD.enabled = false;
                networkDiscoverHUD.enabled = false;
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

            // Ticks
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
