using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Player Status
[System.Serializable]
public class PlayerStatus {
    public Vector3 playerPos;
    public Vector2 prevVel;

    public bool isGrounded;
    public float lastGroundedTime;

    public Vector2 combinedDirection;
    public Vector2 lastCombinedDirection;
    public float propulsionValue;
    public bool wasLastDirNull;

    public bool onGoingJump;
    public bool wasOnGoingJump;
    public bool canceledJump;
    public bool isInAirBecauseOfJump;
    public float lastJumpTime;
    
    public float arealPropulsionMomentum;

    public float lastSubmergedPercentage;
    public float fluidTime;

    public float time;

    public BoundsInt prevTileOverlapBounds;

    public void RemoveTimeRelativity () {
        lastJumpTime -= time;
        lastGroundedTime -= time;
    }
}

public class PlayerInfo {
    public RigidbodyPixel rbody;
    public PlayerStatus status;
}
#endregion

public class PlayerController : MonoBehaviour {
    
    #region References
    [Header("References")]
    public RigidbodyPixel rbody;
    public Collider2D collBox;
    public SpriteRenderer spriteRenderer;
    public PlayerAnimator playerAnimator;

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

    [System.NonSerialized] public PlayerStatus status;
    [System.NonSerialized] public PlayerInfo info;

    public Transform attackIndicator;

    public float timeOfLastAutosave;
    #endregion

    #region MonoBehaviour
    private void Awake () {
        status = new PlayerStatus();
        info = new PlayerInfo {
            rbody = rbody,
            status = status
        };
        currentState = playerStates[0];
    }

    private void OnEnable () {
        if(WorldSaving.inst.LoadPlayerFile(0, out PlayerStatus newStatus)) {
            LoadStatus(newStatus);
        }

        GameManager.inst.allPlayers.Add(this);
    }

    private void FixedUpdate () {
        status.playerPos = transform.position;

        bool isChunkAbsent = true; 
        Vector2Int chunkPos = TerrainManager.inst.WorldToChunk(transform.position);
        long key = Hash.hVec2Int(chunkPos);
        if(ChunkLoader.inst.loadCounters.TryGetValue(key, out ChunkLoadCounter value)) {
            if(value.loadCount > 0) {
                isChunkAbsent = false;
            }
        }

        if(!isChunkAbsent) {
            CheckModifier();
            currentState?.UpdatePlayerStateGroup(info);
        } else {
            rbody.disableForAFrame = true;
        }

        BoundsInt currentTileOverlap = CalculateTileOverlap();
        if(currentTileOverlap != status.prevTileOverlapBounds && GameManager.inst.engineMode == EngineModes.Play) {
            CheckForTileOverlap(currentTileOverlap);
        }
    }

    float timeOfAttack = 0f;
    //Vector2 attackPressScreenPos;
    //Vector2 startDir;

    private void Update () {
        status.time = Time.time;
        Animate();

        if(GameManager.inst.engineMode != EngineModes.Play) {
            attackIndicator.gameObject.SetActive(false);
            return;
        } else {
            attackIndicator.gameObject.SetActive(true);
        }

        Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = (pos - GetHeadPosition()).normalized;
        float angle = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;
        float dist = EntityManager.inst.RaycastEntities(new Ray2D(GetHeadPosition(), dir));
        attackIndicator.position = (GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f));
        attackIndicator.eulerAngles = Vector3.forward * angle;

        if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
            timeOfAttack = Time.time;
        }
        if(Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) {
            Vector2 attackGrab = dir * Mathf.Min(30f, (Time.time - timeOfAttack) * 2);

            if(Input.GetMouseButtonUp(0)) {
                CombatManager.inst.SpawnStrike(
                    this,
                    0,
                    (Vector3)(GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f)),
                    angle,
                    dir,
                    rbody.velocity * 0.5f + attackGrab
                );
            } else {
                CombatManager.inst.SpawnStrike(
                    this,
                    1,
                    (Vector3)(GetHeadPosition() + dir * Mathf.Min(dist + 0.2f, 1.5f)),
                    0f,
                    dir,
                    Vector2.zero
                );
            }
        }
    }
    #endregion

    #region Status Loader
    public void LoadStatus (PlayerStatus newStatus) {
        info.status = newStatus;
        status = newStatus;

        status.RemoveTimeRelativity();

        transform.position = info.status.playerPos;
        rbody.velocity = info.status.prevVel;
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
                BaseTileAsset bta = TerrainManager.inst.tiles.GetTileAssetFromGlobalID(tileGID);
                if(bta.GetType() != typeof(CollectableTileAsset)) {
                    continue;
                }
                ((CollectableTileAsset)bta).OnCollect(x, y);
            }
        }

        status.prevTileOverlapBounds = bounds;
    }
    #endregion

    #region Visual WIP
    float waterWaveCooldown = 0;
    public void Animate () {
        int accDirX = 0;
        if(Input.GetKey(KeyCode.A))
            accDirX--;
        if(Input.GetKey(KeyCode.D))
            accDirX++;

        if(accDirX != 0)
            spriteRenderer.flipX = accDirX > 0;
        
        if(!status.isGrounded) {
            playerAnimator.ChangeState("jump");
        } else {
            if(accDirX == 0) {
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
    public Vector2 GetHeadPosition () {
        return (Vector2)transform.position + rbody.box.size.y * Vector2.up;
    }

    public Vector2 GetCenterPosition () {
        return (Vector2)transform.position + rbody.box.size.y * Vector2.up * 0.5f;
    }
    #endregion

    //WIP SYSTEM
    #region Modifier State Switch
    public void SetCurrentModifiers (int stateID) {
        if(currentState != playerStates[stateID]) {
            status.wasLastDirNull = true;
            status.combinedDirection = Vector2.up;
            status.propulsionValue = 0f;

            currentState = playerStates[stateID];
        }
    }

    void CheckModifier () {
        if(rbody.submergedPercentage > 0.3f) {
            SetCurrentModifiers(1);
        } else {
            SetCurrentModifiers(0);
        }
    }
    #endregion
}

[System.Serializable]
public class GlobalPlayerSettings {
    public float groundHeightCheck = 0.01f;
    public float groundWidthCheckOffset = 0.01f;
    public int groundLayerMask = 18;
}

#region Parameters
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

