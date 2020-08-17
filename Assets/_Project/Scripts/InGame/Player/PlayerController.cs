using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Serialization;

#region Player Status
[System.Serializable]
public class PlayerStatus : AutoBitWritable {

    // Base
    public Vector3 playerPos;
    public Vector2 velocity;
    public float health;

    // Controls
    public int jump;
    public Vector2 dir;
    public Vector2 lookDir;
    public bool isFlying;

    // Ground status
    public bool isGrounded;
    public float groundedTime;

    // Derrived control (No need to be shared)
    public Vector2 combinedDirection;
    public Vector2 lastCombinedDirection;
    public float propulsionValue;
    public bool wasLastDirNull;

    // Jump status
    public bool onGoingJump;
    public bool wasOnGoingJump;
    public bool canceledJump;
    public bool isInAirBecauseOfJump;
    public float jumpTime;
    
    // Areal/Aquatic derrived control and status
    public float arealPropulsionMomentum;
    public float lastSubmergedPercentage;
    public float fluidTime;
}

public class PlayerInfo {
    public RigidbodyPixel rbody;
    public PlayerStatus status;
    public PlayerController pc;
}
#endregion

public class PlayerController : MonoBehaviour {

    #region References
    [Header("References")]
    public PlayerHUD hud;
    public RigidbodyPixel rbody;
    public Collider2D collBox;
    public SpriteRenderer spriteRenderer;
    public PlayerAnimator playerAnimator;
    public Transform playerCenter;
    public LocalPlayer localPlayer;
    public InterpolatedTransform interpolatedTransform;

    public bool isControlledLocally {
        set {
            localControl = value;
            if(localControl) {
                CameraNavigator.inst.playerCenter = playerCenter;
            }
        }
        get {
            return localControl;
        }
    }
    bool localControl = true;

    [Header("New State System")]
    public PlayerStateGroup[] playerStates;
    [HideInInspector] public PlayerStateGroup currentState;

    [Header("Permanent Parameters")]
    public GlobalPlayerSettings playerSettings;

    [Header("Obselete Environnement System")]
    public ControllerParameters controlParam;
    public PhysicsParameters physicsParam;
    public PhysicsEnvironnementModifiers[] modifiers;
    private PhysicsEnvironnementModifiers cmod;

    [Header("Temp")]
    public ParticleSystem[] trailParticles;

    [System.NonSerialized] public PlayerStatus status;
    [System.NonSerialized] public PlayerInfo info;
    BoundsInt prevTileOverlapBounds;

    public float timeOfLastAutosave;

    public const float defaultHealth = 60f;
    public const float healthPerHearts = 12f;

    List<WearableModuleDataPair> oneFrameModules;
    #endregion

    #region MonoBehaviour
    private void Awake () {
        status = new PlayerStatus();
        info = new PlayerInfo {
            rbody = rbody,
            status = status,
            pc = this
        };
        currentState = playerStates[0];

        hud?.BuildHealthContainer(Mathf.CeilToInt(defaultHealth / healthPerHearts));

        oneFrameModules = new List<WearableModuleDataPair>();
    }

    private void FixedUpdate () {
        foreach(ParticleSystem ps in trailParticles) {
            var emitter = ps.emission;
            emitter.enabled = false;
        }

        if(isControlledLocally) {
            status.dir.x = 0;
            status.dir.y = 0;
            status.jump = 0;
            if(Input.GetKey(KeyCode.A))
                status.dir.x--;
            if(Input.GetKey(KeyCode.D))
                status.dir.x++;
            if(Input.GetKey(KeyCode.S))
                status.dir.y--;
            if(Input.GetKey(KeyCode.W))
                status.dir.y++;
            if(Input.GetKey(KeyCode.Space))
                status.jump++;
        }

        status.playerPos = transform.position;
        status.velocity = info.rbody.velocity;

        // Checks if the chunk where the player is at is absent
        bool isChunkAbsent = true; 
        Vector2Int chunkPos = TerrainManager.inst.WorldToChunk(transform.position);
        long key = Hash.longFrom2D(chunkPos);
        if(ChunkLoader.inst.loadCounters.TryGetValue(key, out ChunkLoadCounter value)) {
            if(value.loaders.Count > 0) {
                isChunkAbsent = false;
            }
        }

        // Freezes player in absent chunk and update movement modules
        if(!isChunkAbsent) {
            CheckModifier();
            currentState?.UpdatePlayerStateGroup(info, oneFrameModules);
            oneFrameModules.Clear();
        } else {
            rbody.disableForAFrame = true;
        }
        rbody.disableForAFrame = rbody.disableForAFrame || status.isFlying;

        // Overlap check for tiles with interactable property such as collectables (shells and pearls)
        if(NetworkAssistant.inst.IsServer) {
            BoundsInt currentTileOverlap = CalculateTileOverlap();
            if(currentTileOverlap != prevTileOverlapBounds && GameManager.inst.engineMode == EngineModes.Play) {
                CheckForTileOverlap(currentTileOverlap);
            }
        }
    }

    private void Update () {
        if(Input.GetKeyDown(KeyCode.F) && isControlledLocally) {
            if(GameManager.inst.engineMode == EngineModes.Edit) {
                status.isFlying = true;
                goto end_of_flytoggle;
            }
            if(status.isFlying) {
                status.isFlying = false;
                goto end_of_flytoggle;
            }
            if(NetworkAssistant.inst.clientPlayerDataCopy.permissions.inGameEditingPermissions > InGameEditingPermissions.None) {
                status.isFlying = true;
            }
        }
        end_of_flytoggle:

        Animate();
    }
    #endregion

    #region Status Loader
    public void LoadStatus (PlayerStatus newStatus, bool loadPosition = true) {
        info.status = newStatus;
        status = newStatus;

        if(loadPosition) {
            GetComponent<InterpolatedTransform>().SetTransformPosition(newStatus.playerPos);
            transform.position = info.status.playerPos;
        }
        rbody.velocity = info.status.velocity;
        
        UpdateHealthHud();
    }
    #endregion

    #region TileOverlap
    BoundsInt CalculateTileOverlap () {
        return new BoundsInt() {
            min = Vector3Int.FloorToInt((rbody.aabb.min - (Vector2)PhysicsPixel.inst.queryExtension)),
            max = Vector3Int.FloorToInt((rbody.aabb.max + (Vector2)PhysicsPixel.inst.queryExtension))
        };
    }

    void CheckForTileOverlap (BoundsInt bounds) {
        for(int x = bounds.min.x; x <= bounds.max.x; x++) {
            for(int y = bounds.min.y; y <= bounds.max.y; y++) {
                int tileGID = TerrainManager.inst.GetGlobalIDAt(x, y, TerrainLayers.Ground);
                if(tileGID == 0) {
                    continue;
                }
                BaseTileAsset bta = GeneralAsset.inst.GetTileAssetFromGlobalID(tileGID);
                if(bta.GetType() != typeof(CollectableTileAsset)) {
                    continue;
                }
                ((CollectableTileAsset)bta).OnCollect(x, y);
            }
        }

        prevTileOverlapBounds = bounds;
    }
    #endregion

    #region Visuals
    float waterWaveCooldown = 0;
    void Animate () {
        if(status.dir.x != 0) {
            spriteRenderer.flipX = status.dir.x > 0;
        }
        
        if(!status.isGrounded) {
            playerAnimator.ChangeState("jump");
        } else {
            if(status.dir.x == 0) {
                playerAnimator.ChangeState("idle");
            } else {
                playerAnimator.ChangeState("run");
            }
        }

        if(rbody.submergedPercentage > 0f && status.lastSubmergedPercentage <= 0f) {
            ParticleManager.inst.PlayAdaptiveParticle(transform.position, 0);
        }
        if(rbody.submergedPercentage <= 0f && status.lastSubmergedPercentage > 0f) {
            ParticleManager.inst.PlayAdaptiveParticle(transform.position + Vector3.down, 0);
        }
        if(rbody.submergedPercentage > 0.6f && rbody.submergedPercentage < 0.95f && rbody.velocity.x < -5f && waterWaveCooldown > 0.2f) {
            ParticleManager.inst.PlayAdaptiveParticle(transform.position + Mathf.Max(rbody.submergedPercentage * rbody.box.size.y - 0.25f, 0f) * Vector3.up, 1);
        }
        if(rbody.submergedPercentage > 0.6f && rbody.submergedPercentage < 0.95f && rbody.velocity.x > 5f && waterWaveCooldown > 0.2f) {
            ParticleManager.inst.PlayAdaptiveParticle(transform.position + Mathf.Max(rbody.submergedPercentage * rbody.box.size.y - 0.25f, 0f) * Vector3.up, 2);
        }
        if(waterWaveCooldown > 0.2f) {
            waterWaveCooldown = 0f;
        }
        waterWaveCooldown += Time.deltaTime;


        status.lastSubmergedPercentage = rbody.submergedPercentage;
    }
    #endregion

    #region Public Function
    public void DamagePlayer (Vector2 impactPoint, float healthPoint) {
        status.health = Mathf.Max(0f, status.health - healthPoint);
        UpdateHealthHud();
        ParticleManager.inst.PlayFlytext(impactPoint, Mathf.CeilToInt(healthPoint).ToString(), 0.5f, 2f);
    }

    public void HealPlayer (float healthPoint) {
        status.health = Mathf.Min(defaultHealth, status.health + healthPoint);
        UpdateHealthHud();
    }

    public void SetHealth (float healthPoint) {
        status.health = healthPoint;
        UpdateHealthHud();
    }

    public void UpdateHealthHud () {
        hud?.UpdateHealth(status.health);
    }

    public Vector2 GetHeadPosition () {
        return (Vector2)transform.position + rbody.box.size.y * Vector2.up;
    }

    public Vector2 GetCenterPosition () {
        return (Vector2)transform.position + rbody.box.size.y * Vector2.up * 0.5f;
    }

    public void RunWearableModule (WearableModule module, PlayerModifierData data) {
        oneFrameModules.Add(new WearableModuleDataPair(module, data));
    }

    public void TickTrail (int index) {
        var emitter = trailParticles[index].emission;
        emitter.enabled = true;
    }
    #endregion

    #region Modifier State Switch (WIP)
    public void SetCurrentModifiers (int stateID) {
        if(currentState != playerStates[stateID]) {
            status.wasLastDirNull = true;
            status.combinedDirection = Vector2.up;
            status.propulsionValue = 0f;

            currentState = playerStates[stateID];
        }
    }

    void CheckModifier () {
        if(status.isFlying) {
            SetCurrentModifiers(2);
        } else if(rbody.submergedPercentage > 0.3f) {
            SetCurrentModifiers(1);
        } else {
            SetCurrentModifiers(0);
        }
    }
    #endregion
}

public class WearableModuleDataPair {
    public WearableModule module;
    public PlayerModifierData data;

    public WearableModuleDataPair (WearableModule module, PlayerModifierData data) {
        this.module = module;
        this.data = data;
    }
}

[System.Serializable]
public class GlobalPlayerSettings {
    public float groundHeightCheck = 0.01f;
    public float groundWidthCheckOffset = 0.01f;
    public int groundLayerMask = 18;
}

#region Obselete Parameters
[System.Serializable]
public class ControllerParameters {
    public float initialJumpForce = 6f;
    public float outOfFluidJumpForce = 6f;
    public float cancelJumpForce = 2f;
    public float maxUngroundedJumpTime = 0.075f;
    public float jumpCooldown = 0.1f;

    public float walkAcceleration = 1.1f;
    public float walkMaxSpeed = 2.2f;
    public float runAcceleration = 1.1f;
    public float runMaxSpeed = 2.2f;

    public float arealAcceleration = 10f;
    public float arealMaxSpeed = 8f;

    public float tileJumpForce = 1.3f;
    public float tileJumpCheckDistance = 0.16f;

    public float minFluidJumpTime = 2f;
}

[System.Serializable]
public class PhysicsParameters {
    public float reducedGravityAcceleration = 15f;
    public float augmentedGravityAcceleration = 45f;

    public float groundFriction;
    public float arealFriction;
}

[System.Serializable]
public class PhysicsEnvironnementModifiers {
    public float reducedGravityFactor = 1f;
    public float augmentedGravityFactor = 1f;
    public float groundFrictionFactor = 1;
    public float arealFrictionFactor = 1;

    public bool allowWallSlide = true;
    public bool enableJumpCancel = true;

    public float groundAccelerationFactor = 1f;
    public float groundMaxSpeedFactor = 1f;

    public float arealAccelerationFactor = 1f;
    public float arealMaxSpeedFactor = 1f;

    public bool arealPropulsionEnabled = false;
    public float arealPropulsionAcceleration = 1f;
    public float arealPropulsionMaxSpeed = 1f;
    public float arealPropulsionFriction = 1f;

    public bool fourDirectionControls = false;
    public bool jumpCancelsDownMotion = true;
    public bool useJumpAsUpwards = true;
    public float jumpFactor = 1f;

    public bool useDirectionSmoothing = false;
    public float directionSmoothingSpeed = 3f;
    public float directionSmoothingFactor = 0.1f;
    public float targetToVelRatio = 0.3f;
    public float directionSmoothVelInfluence = 1f;
    public float fluidVelRatio = 0f;

    public bool enablePropulsionModule = false;
    public float propulsionModuleAcc = 1f;
    public float propulsionModuleFriction = 0f;

    public bool tileJumpEnabled = true;
}
#endregion

