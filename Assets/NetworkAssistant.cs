using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Spawning;

public class NetworkAssistant : MonoBehaviour {

    public static NetworkAssistant inst;

    public PlayerController playerPrefab;
    public GameObject gameManagerPrefab;

    public Dictionary<ulong, Transform> playerToAddToChunkLoader = new Dictionary<ulong, Transform>();

    public bool IsServer {
        get {
            return NetworkingManager.Singleton.IsServer;
        }
    }

    public bool IsHost {
        get {
            return NetworkingManager.Singleton.IsHost;
        }
    }

    public ulong ClientID {
        get {
            return NetworkingManager.Singleton.LocalClientId;
        }
    }

    private void Start () {
        inst = this;

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
    }

    private void OnClientDisconnected (ulong clientID) {
        Debug.Log("Client left: " + clientID);
    }


    public void StartClient (string ip, ushort port) {
        var transp = ((RufflesTransport.RufflesTransport)NetworkingManager.Singleton.NetworkConfig.NetworkTransport);
        var config = NetworkingManager.Singleton.NetworkConfig;

        transp.ConnectAddress = ip;
        transp.Port = port;
        config.RegisteredScenes.Add("Main");
        NetworkingManager.Singleton.StartClient();
    }

    public void StartHost () {
        var transp = ((RufflesTransport.RufflesTransport)NetworkingManager.Singleton.NetworkConfig.NetworkTransport);
        var config = NetworkingManager.Singleton.NetworkConfig;

        string ip = "127.0.0.1";
        ushort port = 7777;

        transp.ConnectAddress = ip;
        transp.Port = port;
        config.RegisteredScenes.Add("Main");
        NetworkingManager.Singleton.StartHost(Vector3.zero, Quaternion.identity, true, SpawnManager.GetPrefabHashFromGenerator("LocalPlayer"));
    }
}
