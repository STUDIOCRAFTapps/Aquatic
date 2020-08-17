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
                //player.interpolatedTransform.useNetworkControl = true;
            }

            if(IsServer && TerrainManager.inst == null) {
                Instantiate(NetworkAssistant.inst.gameManagerPrefab).GetComponent<NetworkedObject>().Spawn(null, true);
            }

            if(ChunkLoader.inst == null) {
                NetworkAssistant.inst.playerToAddToChunkLoader.Add(OwnerClientId, player.playerCenter);
            } else {
                ChunkLoader.inst.AddPlayer(OwnerClientId, player.playerCenter);
            }

            if(IsLocalPlayer && IsHost) {
                if(WorldSaving.inst.LoadPlayerFile(0, out PlayerStatus playerStatus)) {
                    player.LoadStatus(playerStatus);
                }
            }
            player.isControlledLocally = IsLocalPlayer;
            lastSentPos = player.transform.position;

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

            ChunkLoader.inst.RemovePlayer(OwnerClientId);
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
        bool rollbackPlayer = false;

        if(!(NetworkAssistant.inst.IsHost && ExecutingRpcSender == NetworkAssistant.inst.ClientID)) {
            PlayerPermissions perms = NetworkAssistant.inst.serverPlayerData[ExecutingRpcSender].permissions;

            // If inside wall while playing, revert position
            if(GameManager.inst.engineMode == EngineModes.Play && perms.inGameEditingPermissions == InGameEditingPermissions.None) {
                if(PhysicsPixel.inst.BoundsCast(player.rbody.GetBoundFromColliderFakePosition(status.playerPos))) {
                    status.playerPos = player.status.playerPos;
                    status.velocity = player.status.velocity;
                    rollbackPlayer = true;
                }
                if(status.isFlying) {
                    status.playerPos = player.status.playerPos;
                    status.velocity = player.status.velocity;
                    status.isFlying = false;
                    rollbackPlayer = true;
                }
            }
            if(GameManager.inst.engineMode == EngineModes.Edit) {
                if(!status.isFlying) {
                    status.playerPos = player.status.playerPos;
                    status.velocity = player.status.velocity;
                    status.isFlying = true;
                    rollbackPlayer = true;
                }
            }
            player.LoadStatus(status, false);
        }

        if(!rollbackPlayer) {
            InvokeClientRpcOnEveryoneExcept(PlayerStatusClient, ExecutingRpcSender, status, "Player");
        } else {
            InvokeClientRpcOnEveryone(PlayerStatusClient, status, "Player");
        }
        
    }

    [ClientRPC]
    private void PlayerStatusClient (PlayerStatus status) {
        correctionDelta += (Vector2)status.playerPos - (Vector2)player.transform.position;
        player.LoadStatus(status, true);
        
        if(lastRecievedTime != Time.unscaledTime) {
            if(posInitied) {
                AddIntervalSample(Time.unscaledTime - lastRecievedTime);
            }
            lerpStartPos = correctionDelta;
        } else {
            //Debug.LogWarning($"Multiple packets sent {status.playerPos}");
            lerpStartPos = correctionDelta;
        }
        lerpEndPos = Vector2.zero;
        lerpT = 0;
        
        lastRecievedTime = Time.unscaledTime;

        if(!posInitied) {
            correctionDelta = Vector2.zero;
            posInitied = true;
            lerpStartPos = lerpEndPos;
        }
    }

    public void SyncPermisionsFromServer (ulong clientId, PlayerPermissions permissions) {
        InvokeClientRpcOnClient(SyncPermisions, clientId, permissions, "Player");
    }

    [ClientRPC]
    void SyncPermisions (PlayerPermissions permissions) {
        NetworkAssistant.inst.clientPlayerDataCopy.permissions = permissions;
    }

    const int SendsPerSecond = 20;
    const bool ExtrapolatePosition = false;
    const float MaxSendsToExtrapolate = 5;
    private float sendT;
    private float lerpT;

    private Vector3 lerpStartPos;
    private Vector3 lerpEndPos;
    public Vector2 correctionDelta;

    private float lastSendTime;
    private Vector3 lastSentPos;

    private bool posInitied = false;
    private float lastRecievedTime;

    const int IntervalSamples = 20;
    private List<float> intervalAverage = new List<float>();
    bool recalculateAverage = true;
    float lastAverage = 0f;
    
    public void PlayerNetworkInterpolation () {
        if(player.rbody.isCollidingDown) {
            correctionDelta.y = 0f;
        }

        if(IsOwner) {
            sendT += Time.deltaTime;

            if(sendT > (1f / SendsPerSecond)) {
                sendT = 0f;

                InvokeServerRpc(PlayerStatusServer, player.status);
            }
        } else {
            float sendDelay = CalculateIntervalAverage() * 2f;
            lerpT += Time.unscaledDeltaTime / sendDelay;

            if(ExtrapolatePosition && Time.unscaledTime - lastRecievedTime < sendDelay * MaxSendsToExtrapolate)
                correctionDelta = Vector2.LerpUnclamped(lerpStartPos, lerpEndPos, lerpT);
            else
                correctionDelta = Vector2.Lerp(lerpStartPos, lerpEndPos, lerpT);

            player.interpolatedTransform.SetOffset(-correctionDelta);
        }
    }

    [HideInInspector]
    public AnimationCurve DistanceSendrate = AnimationCurve.Constant(0, 500, 20);
    private float GetTimeForLerp (Vector3 pos1, Vector3 pos2) {
        return 1f / DistanceSendrate.Evaluate(Vector3.Distance(pos1, pos2));
    }

    float CalculateIntervalAverage () {
        if(recalculateAverage) {
            recalculateAverage = false;
            if(intervalAverage.Count == 0f) {
                lastAverage = (1f / SendsPerSecond);
            } else {
                float total = 0;
                for(int i = 0; i < intervalAverage.Count; i++) {
                    total += intervalAverage[i];
                }
                lastAverage = total / intervalAverage.Count;
            }
        }
        return lastAverage;
    }

    void AddIntervalSample (float sample) {
        if(intervalAverage.Count >= IntervalSamples) {
            intervalAverage.RemoveAt(0);
        }
        intervalAverage.Add(sample);
        recalculateAverage = true;
    }
}
