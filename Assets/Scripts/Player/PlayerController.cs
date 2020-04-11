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

    public void RemoveTimeRelativity (float currentTime) {
        lastJumpTime -= currentTime;
        lastGroundedTime -= currentTime;
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

    private void FixedUpdate () {
        status.playerPos = transform.position;

        bool isChunkAbsent = true; 
        Vector2Int chunkPos = TerrainManager.inst.WorldToChunk(transform.position);
        if(ChunkLoader.inst.loadCounters.ContainsKey(chunkPos)) {
            if(ChunkLoader.inst.loadCounters[chunkPos].loadCount > 0) {
                isChunkAbsent = false;
            }
        }

        if(!isChunkAbsent) {
            CheckModifier();
            currentState?.UpdatePlayerStateGroup(info);
        }

        /*CheckIfGrounded();
        CheckFluidTime();

        ApplyFriction();
        ApplyMovement();
        JumpCheck();
        StairJumpCheck();
        ArealPropulsion();
        ApplyGravity();*/
    }

    private void Update () {
        Animate();
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
            ParticleManager.inst.PlayEntityParticle(transform.position, 4);
        }
        if(rbody.submergedPercentage <= 0f && status.lastSubmergedPercentage > 0f) {
            ParticleManager.inst.PlayEntityParticle(transform.position + Vector3.down, 4);
        }
        if(rbody.submergedPercentage > 0.6f && rbody.submergedPercentage < 0.95f && rbody.velocity.x < -5f && waterWaveCooldown > 0.2f) {
            ParticleManager.inst.PlayEntityParticle(transform.position + Mathf.Max(rbody.submergedPercentage * rbody.box.size.y - 0.25f, 0f) * Vector3.up, 5);
        }
        if(rbody.submergedPercentage > 0.6f && rbody.submergedPercentage < 0.95f && rbody.velocity.x > 5f && waterWaveCooldown > 0.2f) {
            ParticleManager.inst.PlayEntityParticle(transform.position + Mathf.Max(rbody.submergedPercentage * rbody.box.size.y - 0.25f, 0f) * Vector3.up, 6);
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
    #endregion

    //OBSELETE SYSTEM
    /*
    #region General Status Updater
    void UpdateStatus () {
        UpdateJumpStatus();
    }

    void UpdateJumpStatus () {
        bool wasOnGoingJump = status.onGoingJump;
        status.onGoingJump = Input.GetKey(KeyCode.Space);
        if(wasOnGoingJump && !status.onGoingJump) {
            status.canceledJump = true;
        }
    }
    #endregion
    #region Actions
    void JumpCheck () {
        // Jump button
        bool wasOnGoingJump = status.onGoingJump;
        status.onGoingJump = Input.GetKey(KeyCode.Space);
        if(wasOnGoingJump && !status.onGoingJump) {
            status.canceledJump = true;
        }

        // Jump Timers
        float jumpCooldownTimer = Time.unscaledTime - status.lastJumpTime;
        float ungroundedCooldownTimer = Time.unscaledTime - status.lastGroundedTime;

        // Jump Checks
        bool jumpOptReq0 = status.isGrounded;
        bool jumpOptReq1 = (ungroundedCooldownTimer < controlParam.maxUngroundedJumpTime && !status.isInAirBecauseOfJump);
        bool jumpOptReq2 = (rbody.submergedPercentage > 0.5f && rbody.submergedPercentage < 0.6f) && status.fluidTime > controlParam.minFluidJumpTime && !status.isGrounded;
        bool jumpReq0 = status.onGoingJump;
        bool jumpReq1 = (jumpCooldownTimer > controlParam.jumpCooldown);

        // Execute action
        bool canJump = jumpReq0 && jumpReq1 && (jumpOptReq0 || jumpOptReq1 || jumpOptReq2);
        if(canJump) {
            rbody.velocity.y = 0;
            rbody.velocity += Vector2.up * ((jumpOptReq2) ? controlParam.outOfFluidJumpForce : controlParam.initialJumpForce) * cmod.jumpFactor;
            status.lastJumpTime = Time.unscaledTime;
            status.isInAirBecauseOfJump = true;
        }
    }

    void ArealPropulsion () {
        if(status.onGoingJump && cmod.arealPropulsionEnabled) {
            status.arealPropulsionMomentum = 1f;
        }
        AccelerateBody(Vector2.up, cmod.arealPropulsionMaxSpeed * status.arealPropulsionMomentum, cmod.arealPropulsionAcceleration * status.arealPropulsionMomentum);
        status.arealPropulsionMomentum *= (1f - Time.deltaTime * cmod.arealPropulsionFriction);
    }

    void StairJumpCheck () {
        //Stair Jump Conditions
        if(!((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && status.isGrounded && !status.onGoingJump)) {
            return;
        }
        if(!cmod.tileJumpEnabled) {
            return;
        }

        //Sample Directions
        int moveDirection = GetMoveDirection();
        PhysicsPixel.Axis axis = MoveDirectionToAxis(moveDirection);

        // Calculate Wall Ray Origin
        Vector2 wallRayOrigin = new Vector2(
            transform.position.x + (collBox.bounds.size.x * moveDirection * 0.5f),
            transform.position.y
        ) + Vector2.up * 0.01f;

        // Check if the wall is in a resonable distance
        bool wallRaycastHit = PhysicsPixel.inst.AxisAlignedRaycast(wallRayOrigin, axis, controlParam.tileJumpCheckDistance, out Vector2 wallRaycastPoint);
        if(!wallRaycastHit) {
            return;
        }

        // Calculate land region space
        Bounds bnds = new Bounds();
        if(axis == PhysicsPixel.Axis.Left) {
            bnds.SetMinMax(
                new Vector2(wallRaycastPoint.x - 0.5f, Mathf.Floor(wallRayOrigin.y / 1f) * 1f + 1f),
                new Vector2(wallRaycastPoint.x, Mathf.Floor(wallRayOrigin.y / 1f) * 1f + 3f)
            );
        } else {
            bnds.SetMinMax(
                new Vector2(wallRaycastPoint.x, Mathf.Floor(wallRayOrigin.y / 1f) * 1f + 1f),
                new Vector2(wallRaycastPoint.x + 0.5f, Mathf.Floor(wallRayOrigin.y / 1f) * 1f + 3f)
            );
        }

        // Check if landing region isn't blocked
        bool landRegionBlocked = PhysicsPixel.inst.BoundsCast(bnds);
        if(landRegionBlocked) {
            return;
        }

        // Figure out the height of the stair and calculate the velocity for the inverse kinematic
        wallRayOrigin.Set(wallRaycastPoint.x + 0.25f * moveDirection, wallRaycastPoint.y + 2.875f);
        bool stairHeightRaycastHit = PhysicsPixel.inst.AxisAlignedRaycast(wallRayOrigin, PhysicsPixel.Axis.Down, 2.875f, out Vector2 stairPoint);
        if(stairHeightRaycastHit) {
            float tileJumpBase = Mathf.Sqrt(2f * physicsParam.augmentedGravityAcceleration * (stairPoint.y - transform.position.y));
            rbody.velocity += Vector2.up * tileJumpBase * controlParam.tileJumpForce * (stairPoint.y - transform.position.y);
        }
    }
    #endregion
    #region Motion
    void ApplyGravity () {
        if(status.onGoingJump && rbody.velocity.y > 0) {
            status.canceledJump = false;
            rbody.velocity += physicsParam.reducedGravityAcceleration * cmod.reducedGravityFactor * Vector2.down * Time.fixedDeltaTime;
        } else if(status.canceledJump && (rbody.velocity.y > 0) && cmod.enableJumpCancel) {
            rbody.velocity += controlParam.cancelJumpForce * Vector2.down;
        } else {
            rbody.velocity += physicsParam.augmentedGravityAcceleration * cmod.augmentedGravityFactor * Vector2.down * Time.fixedDeltaTime;
        }
    }

    void ApplyMovement () {
        int accDirX = 0;
        int accDirY = 0;
        if(Input.GetKey(KeyCode.A))
            accDirX--;
        if(Input.GetKey(KeyCode.D))
            accDirX++;
        if(Input.GetKey(KeyCode.W) || (cmod.useJumpAsUpwards && status.onGoingJump))
            accDirY++;
        if(Input.GetKey(KeyCode.S) && (!cmod.jumpCancelsDownMotion || !status.onGoingJump))
            accDirY--;
        if(accDirX != 0)
            spriteRenderer.flipX = accDirX > 0;

        // Compute direction
        #region Direction
        if(cmod.useDirectionSmoothing) {
            Vector2 targetDir = new Vector2(accDirX, cmod.fourDirectionControls ? accDirY : 0f);
            float blend = 1f - Mathf.Pow(1f - cmod.directionSmoothingFactor, Time.deltaTime * cmod.directionSmoothingSpeed);

            bool isCurrentDirectionNull = false;
            if(cmod.fourDirectionControls) {
                isCurrentDirectionNull = accDirX == 0 && accDirY == 0;
            } else {
                isCurrentDirectionNull = accDirX == 0;
            }

            if(status.wasLastDirNull && !isCurrentDirectionNull) {
                status.combinedDirection = Vector2.Lerp(targetDir, rbody.velocity.normalized, rbody.velocity.magnitude * cmod.directionSmoothVelInfluence);
            } else if(Vector2.Dot(status.combinedDirection, targetDir) < -0.25f) {
                status.combinedDirection = Vector2.Lerp(targetDir, rbody.velocity.normalized, rbody.velocity.magnitude * cmod.directionSmoothVelInfluence);
            } if(!isCurrentDirectionNull) {
                status.combinedDirection = Quaternion.Slerp(
                    Quaternion.LookRotation(Vector3.forward, status.combinedDirection),
                    Quaternion.LookRotation(Vector3.forward, targetDir),
                    blend
                ) * Vector3.up;
                Debug.DrawLine(transform.position, transform.position + Quaternion.LookRotation(Vector3.forward, status.combinedDirection) * Vector3.up, Color.red);
            } else {
                status.combinedDirection = Vector2.zero;
            }

            // Slide around world
            if(targetDir.y < 0 && (rbody.isCollidingLeft || rbody.isCollidingRight)) {
                status.combinedDirection = Vector2.down;
            }
            if(targetDir.y > 0 && (rbody.isCollidingLeft || rbody.isCollidingRight)) {
                status.combinedDirection = Vector2.up;
            }
            // Slide around world
            if(targetDir.x < 0 && (rbody.isCollidingUp || rbody.isCollidingDown)) {
                status.combinedDirection = Vector2.left;
            }
            if(targetDir.x > 0 && (rbody.isCollidingUp || rbody.isCollidingDown)) {
                status.combinedDirection = Vector2.right;
            }


            if(cmod.fourDirectionControls) {
                status.wasLastDirNull = accDirX == 0 && accDirY == 0;
            } else {
                status.wasLastDirNull = accDirX == 0;
            }
        } else {
            status.combinedDirection = new Vector2(accDirX, cmod.fourDirectionControls ? accDirY:0f);
        }
        #endregion

        #region Propulsion
        Vector2 wallNormal = new Vector2(
            (rbody.isCollidingLeft ? -1f : 0f) + (rbody.isCollidingRight ? 1f : 0f),
            (rbody.isCollidingDown ? -1f : 0f) + (rbody.isCollidingUp ? 1f : 0f)
        );

        if(accDirX != 0f || accDirY != 0f) {
            if(wallNormal == Vector2.zero) {
                status.propulsionValue += cmod.propulsionModuleAcc * Time.deltaTime;
            } else {
                status.propulsionValue += cmod.propulsionModuleAcc * Time.deltaTime * Mathf.Min(1f, (-Vector2.Dot(wallNormal, status.combinedDirection) + 1f));
            }
        }
        status.propulsionValue *= (1f - Time.fixedDeltaTime * cmod.propulsionModuleFriction);

        //reduce direction if directly going at a wall
        #endregion

        // Apply acceleration
        #region Acceleration
        if(status.isGrounded) {
            AccelerateBody(
                status.combinedDirection,
                controlParam.walkMaxSpeed * cmod.groundMaxSpeedFactor,
                controlParam.walkAcceleration * cmod.groundAccelerationFactor
            );
        } else {
            if(cmod.useDirectionSmoothing) {
                if(cmod.enablePropulsionModule) {
                    rbody.velocity += status.combinedDirection * status.propulsionValue;
                } else {
                    AccelerateBody(
                        status.combinedDirection,
                        controlParam.arealMaxSpeed * cmod.arealMaxSpeedFactor,
                        controlParam.arealAcceleration * cmod.arealAccelerationFactor
                    );
                }
            } else {
                AccelerateBody(
                    status.combinedDirection,
                    controlParam.arealMaxSpeed * cmod.arealMaxSpeedFactor,
                    controlParam.arealAcceleration * cmod.arealAccelerationFactor
                );
            }
        }
        #endregion
    }

    void ApplyFriction () {
        // Friction
        if(status.isGrounded && !(status.isInAirBecauseOfJump && status.onGoingJump)) {
            rbody.velocity *= (1f - Time.fixedDeltaTime * physicsParam.groundFriction * cmod.groundFrictionFactor);
        } else {
            rbody.velocity *= (1f - Time.fixedDeltaTime * physicsParam.arealFriction * cmod.arealFrictionFactor);
        }

        // Wall slide
        int accDirX = 0;
        if(Input.GetKey(KeyCode.A))
            accDirX--;
        if(Input.GetKey(KeyCode.D))
            accDirX++;
    }

    void CheckFluidTime () {
        if(rbody.submergedPercentage > 0.1f) {
            status.fluidTime += Time.deltaTime;
        } else {
            status.fluidTime = 0f;
        }
    }
    #endregion
*/

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

