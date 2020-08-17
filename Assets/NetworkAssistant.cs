using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Spawning;
using MLAPI.Messaging;

public class NetworkAssistant : MonoBehaviour {

    public static NetworkAssistant inst;

    public PlayerController playerPrefab;
    public GameObject gameManagerPrefab;

    public Dictionary<ulong, Transform> playerToAddToChunkLoader = new Dictionary<ulong, Transform>();

    public Dictionary<ulong, ServerPlayerData> serverPlayerData = new Dictionary<ulong, ServerPlayerData>();
    public ServerPlayerData clientPlayerDataCopy;

    public bool IsServer {
        get {
            return NetworkingManager.Singleton.IsServer;
        }
    }

    public float Time {
        get {
            return NetworkingManager.Singleton.NetworkTime;
        }
    }

    public bool IsHost {
        get {
            return NetworkingManager.Singleton.IsHost;
        }
    }

    public bool IsClient {
        get {
            return NetworkingManager.Singleton.IsClient;
        }
    }

    public bool IsClientNotHost {
        get {
            return NetworkingManager.Singleton.IsClient && !IsHost;
        }
    }

    public ulong ClientID {
        get {
            return NetworkingManager.Singleton.LocalClientId;
        }
    }

    #region Init & Connection
    private void Start () {
        inst = this;

        clientPlayerDataCopy = new ServerPlayerData("Player", ulong.MaxValue, new PlayerPermissions());
        clientPlayerDataCopy.permissions.SetLowPermission();

        NetworkingManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkingManager.Singleton.OnClientConnectedCallback += OnClientConnect;
        NetworkingManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void ApprovalCheck (byte[] connectionData, ulong clientId, NetworkingManager.ConnectionApprovedDelegate callback) {
        // Your logic here EDIT: No logic to be done for now
        bool approve = true;
        bool createPlayerObject = true;

        ulong prefabHash = SpawnManager.GetPrefabHashFromGenerator("LocalPlayer");

        // If approve is true, the connection gets added. If it's false. The client gets disconnected
        callback(createPlayerObject, prefabHash, approve, Vector3.zero, Quaternion.identity);
    }

    private void OnClientConnect (ulong clientID) {
        Debug.Log("Client joined: " + clientID);

        if(IsServer) {
            serverPlayerData.Add(
                clientID,
                new ServerPlayerData(
                    "Player",
                    clientID,
                    new PlayerPermissions()
                )
            );
            serverPlayerData[clientID].permissions.SetHighPermission();

            NetworkingManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<LocalPlayer>().SyncPermisionsFromServer(clientID, serverPlayerData[clientID].permissions);
        }
    }

    private void OnClientDisconnected (ulong clientID) {
        Debug.Log("Client left: " + clientID);

        if(IsServer) {
            serverPlayerData.Remove(clientID);
        } else {
            PromptConfigurator.QueuePromptText("Connection failed", 
                "No connection was made to the ip you requested. " +
                "Check if the ip is correct, if the server is up or if your firewall is enabled. ",
                () => {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("SavesMenu");
                });
        }
    }

    // Called by the ui button
    public void StartClient (string ip, ushort port) {
        var transp = ((RufflesTransport.RufflesTransport)NetworkingManager.Singleton.NetworkConfig.NetworkTransport);
        var config = NetworkingManager.Singleton.NetworkConfig;

        transp.ConnectAddress = ip;
        transp.Port = port;
        config.RegisteredScenes.Add("Main");
        NetworkingManager.Singleton.StartClient();
    }

    // TODO: Custom port
    public void StartServer () {
        var transp = ((RufflesTransport.RufflesTransport)NetworkingManager.Singleton.NetworkConfig.NetworkTransport);
        var config = NetworkingManager.Singleton.NetworkConfig;

        string ip = "127.0.0.1";
        ushort port = 7777;

        transp.ConnectAddress = ip;
        transp.Port = port;
        config.RegisteredScenes.Add("Main");
        NetworkingManager.Singleton.StartServer();

        serverPlayerData = new Dictionary<ulong, ServerPlayerData>();
    }

    // TODO: Custom port
    public void StartHost () {
        var transp = ((RufflesTransport.RufflesTransport)NetworkingManager.Singleton.NetworkConfig.NetworkTransport);
        var config = NetworkingManager.Singleton.NetworkConfig;

        string ip = "127.0.0.1";
        ushort port = 7777;

        transp.ConnectAddress = ip;
        transp.Port = port;
        config.RegisteredScenes.Add("Main");
        NetworkingManager.Singleton.StartHost(Vector3.zero, Quaternion.identity, true, SpawnManager.GetPrefabHashFromGenerator("LocalPlayer"));

        OnClientConnect(ClientID);
    }
    #endregion
}

public class ServerPlayerData {
    public string username;
    public ulong clientID;
    public PlayerPermissions permissions;

    public ServerPlayerData (string username, ulong clientID, PlayerPermissions permissions) {
        this.username = username;
        this.clientID = clientID;
        this.permissions = permissions;
    }
}
