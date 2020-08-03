using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.Messaging;

public class LocalPlayer : NetworkedBehaviour {
    
    public PlayerController player;

    #region Scene and Controller Spawning
    private void Update () {
        PlayerNetworkInterpolation();
    }

    public override void NetworkStart () {

        DontDestroyOnLoad(this);

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnSceneLoaded (Scene scene, LoadSceneMode loadSceneMode) {
        if(scene.name == "Main") {
            player = Instantiate(NetworkAssistant.inst.playerPrefab);
            player.localPlayer = this;


            if(!IsOwner) {
                player.interpolatedTransform.useNetworkControl = true;
            }

            if(IsServer && TerrainManager.inst == null) {
                Instantiate(NetworkAssistant.inst.gameManagerPrefab).GetComponent<NetworkedObject>().Spawn(null, true);
            }

            if(ChunkLoader.inst == null) {
                NetworkAssistant.inst.playerToAddToChunkLoader.Add(OwnerClientId, player.playerCenter);
            } else {
                ChunkLoader.inst.PlayerJoins(OwnerClientId, player.playerCenter);
            }

            if(IsLocalPlayer && IsHost) {
                if(WorldSaving.inst.LoadPlayerFile(0, out PlayerStatus playerStatus)) {
                    player.LoadStatus(playerStatus);
                }
            }
            player.isControlledLocally = IsLocalPlayer;

            lastSentPos = player.transform.position;
            lerpStartPos = player.transform.position;
            lerpEndPos = player.transform.position;

            GameManager.inst.allPlayers.Add(player);
        }
    }

    void OnDestroy () {
        CleanPlayer();
    }

    void CleanPlayer () {
        if(player != null) {
            if(NetworkAssistant.inst.playerToAddToChunkLoader.ContainsKey(OwnerClientId)) {
                NetworkAssistant.inst.playerToAddToChunkLoader.Remove(OwnerClientId);
            }

            ChunkLoader.inst.PlayerLeaves(OwnerClientId);
            Destroy(player.gameObject);
            GameManager.inst.allPlayers.Remove(player);
        }
    }

    void OnSceneUnloaded (Scene scene) {
        CleanPlayer();
    }
    #endregion

    [ServerRPC(RequireOwnership = true)]
    private void PlayerStatusServer (PlayerStatus status) {
        InvokeClientRpcOnEveryoneExcept(PlayerStatusClient, ExecutingRpcSender, status);
    }

    [ClientRPC]
    private void PlayerStatusClient (PlayerStatus status) {
        player.LoadStatus(status, false);

        lastRecieveTime = Time.unscaledTime;
        lerpStartPos = player.transform.position;
        lerpEndPos = status.playerPos;
        lerpT = 0;
    }

    const int SendsPerSecond = 20;
    const bool ExtrapolatePosition = false;
    const float MaxSendsToExtrapolate = 5;
    private float sendT;
    private float lerpT;

    private Vector3 lerpStartPos;
    private Vector3 lerpEndPos;

    private float lastSendTime;
    private Vector3 lastSentPos;

    private float lastRecieveTime;

    public void PlayerNetworkInterpolation () {
        if(IsOwner) {
            sendT += Time.deltaTime;

            if(sendT > (1f / SendsPerSecond)) {
                sendT = 0f;

                InvokeServerRpc(PlayerStatusServer, player.status);
            }
        } else {
            float sendDelay = (1f / SendsPerSecond);//(IsServer || NetworkingManager.Singleton.ConnectedClients[NetworkingManager.Singleton.LocalClientId].PlayerObject == null) ? (1f / SendsPerSecond) : GetTimeForLerp(transform.position, NetworkingManager.Singleton.ConnectedClients[NetworkingManager.Singleton.LocalClientId].PlayerObject.transform.position);
            lerpT += Time.unscaledDeltaTime / sendDelay;

            if(ExtrapolatePosition && Time.unscaledTime - lastRecieveTime < sendDelay * MaxSendsToExtrapolate)
                player.transform.position = Vector3.LerpUnclamped(lerpStartPos, lerpEndPos, lerpT);
            else
                player.transform.position = Vector3.Lerp(lerpStartPos, lerpEndPos, lerpT);
        }
    }

    [HideInInspector]
    public AnimationCurve DistanceSendrate = AnimationCurve.Constant(0, 500, 20);
    private float GetTimeForLerp (Vector3 pos1, Vector3 pos2) {
        return 1f / DistanceSendrate.Evaluate(Vector3.Distance(pos1, pos2));
    }
}
