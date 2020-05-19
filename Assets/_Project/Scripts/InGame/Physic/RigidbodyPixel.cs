using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class RigidbodyPixel : MonoBehaviour {

    #region Header
    [Header("Physics Parameters")]
    public float mass = 1f;
    public float bounciness = 1f;
    public float terrainBounciness = 0f;
    public float defaultFloorFriction = 0f;
    public float defaultWallFriction = 0f;

    [Header("Parenting Parameters")]
    public bool canBeParentPlatform = true;
    public bool reorderChild = false;
    public bool interactsWithComplexCollider = false;

    [Header("Collider Type Parameters")]
    public bool collidesOnlyWithTerrain = false;
    public bool isComplexCollider = false;
    public bool applyGenericGravityForceOnLoad = false;
    public bool eliminatesPenetration = false;
    public bool secondExecutionOrder = false;
    public bool clipPermision = false;
    public float clipAmout = 0.25f;
    public bool weakPushCandidate = true;

    // Privates
    private bool failedInitialization;
    private List<Bounds2D> sampledCollisions;
    private RigidbodyPixel parentPlatform;
    private bool isParentPlaformConn = false;
    private RigidbodyPixel previousParentPlatform;
    private bool isPrevParentPlaformConn = false;
    private Vector2 pVelDir;
    private float totalVolume = 0f;
    private float subtractedVolume = 0f;
    private List<ForcePixel> forces;
    public float superPushForce = 0f;

    // Hidden Variables
    [HideInInspector] public BoxCollider2D box;
    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public Bounds2D aabb;
    [HideInInspector] public float inverseMass = 0f;
    [HideInInspector] public Vector2 movementDelta;
    [HideInInspector] public MobileChunk mobileChunk;
    [HideInInspector] public Transform aligmentObject;
    [HideInInspector] public bool disableForAFrame;

    [HideInInspector] public bool hadCollisionDown;
    [HideInInspector] public bool hadCollisionLeft;
    [HideInInspector] public bool hadCollisionUp;
    [HideInInspector] public bool hadCollisionRight;

    [Header("Colliding Output")]
    public bool isCollidingDown;
    public bool isCollidingLeft;
    public bool isCollidingUp;
    public bool isCollidingRight;

    public bool isCollidingWallDown;
    public bool isCollidingWallLeft;
    public bool isCollidingWallUp;
    public bool isCollidingWallRight;
    public RigidbodyPixel isInsideComplexObject;

    [Header("Fluid Output")]
    public float submergedPercentage = 0f;
    public float volume = 0f;

    Vector2 lastVel;
    bool hasBeenInit;
    #endregion


    #region Monobehaviour
    private void Start () {
        Init();
    }

    public void Init () {
        if(hasBeenInit) {
            return;
        }
        
        hasBeenInit = true;

        // Adding rigibody to the global rigibody registery
        PhysicsPixel.inst.rbs.Add(this);

        // Getting all necessairy components
        if(box == null)
            box = GetComponent<BoxCollider2D>();
        mobileChunk = GetComponent<MobileChunk>();
        forces = new List<ForcePixel>();
        GetComponents(forces);
        if(GetComponent<Rigidbody2D>()) {
            Debug.Log("There's a rigidbody 2D attached to this object. You can't have multiple rigidbody types on one object");
            failedInitialization = true;
        }
        if(applyGenericGravityForceOnLoad) {
            if(forces.Count <= 0) {
                gameObject.AddComponent<ForcePixel>();
                gameObject.GetComponents(forces);
            }

            forces[0].force = PhysicsPixel.inst.genericGravityForce;
        }

        // Prepare sampled collisions chached array
        sampledCollisions = new List<Bounds2D>();
        if(!isComplexCollider) {
            aabb = GetBoundFromCollider();
        } else {
            aabb = GetBoundFromCollider((Vector2)mobileChunk.mobileDataChunk.restrictedSize);
        }
    }

    private void OnDestroy () {
        PhysicsPixel.inst.rbs.Remove(this);
    }
    #endregion

    #region Simulate Step
    // Apply force components
    public void ApplyForces () {
        // Prepare forces so they can be eliminated if not needed
        foreach(ForcePixel fp in forces) {
            if(!fp.enabled) {
                continue;
            }
            velocity += fp.force * Time.fixedDeltaTime;
            velocity *= (1f - Time.fixedDeltaTime * fp.constantFriction);
        }
    }

    // Apply special buoyancy forces
    public void ApplyBuoyancy () {
        velocity += (submergedPercentage * volume) * PhysicsPixel.inst.fluidMassPerUnitDensity * -PhysicsPixel.inst.genericGravityForce * inverseMass * Time.deltaTime;
        velocity *= Mathf.Lerp(1f, (1f - Time.fixedDeltaTime * PhysicsPixel.inst.fluidDrag), submergedPercentage);
    }

    public void SimulateFixedUpdate () {
        if(failedInitialization) return;

        // Apply parent's velocity when leaving the parent
        if(previousParentPlatform != null && parentPlatform == null) {
            velocity += previousParentPlatform.velocity;
        }

        // Calculate inverse mass
        if(mass != 0) {
            inverseMass = 1f / mass;
        } else {
            inverseMass = 0f;
        }

        // Reset collision checks
        isCollidingDown = false;
        isCollidingLeft = false;
        isCollidingUp = false;
        isCollidingRight = false;

        isCollidingWallDown = false;
        isCollidingWallLeft = false;
        isCollidingWallUp = false;
        isCollidingWallRight = false;
        movementDelta = Vector2.zero;

        // Move the rigibody
        lastVel = velocity;
        ApplyExternalCollisionInfo();
        ApplyVelocity();
        if(mobileChunk != null) {
            mobileChunk.UpdatePositionData(transform.position);
        }

        if(clipPermision) {
            CheckForClipping();
        }

        // Cycle temp. variables
        isPrevParentPlaformConn = isParentPlaformConn;
        previousParentPlatform = parentPlatform;
        isParentPlaformConn = false;
        parentPlatform = null;
    }

    public void ApplyExternalCollisionInfo () {
        if(hadCollisionDown) {
            isCollidingDown = true;
            hadCollisionDown = false;
        }
        if(hadCollisionLeft) {
            isCollidingLeft = true;
            hadCollisionLeft = false;
        }
        if(hadCollisionUp) {
            isCollidingUp = true;
            hadCollisionUp = false;
        }
        if(hadCollisionRight) {
            isCollidingRight = true;
            hadCollisionRight = false;
        }
    }
    #endregion


    #region External Functions
    /// <summary>
    /// Moves the rigidbody to a certain position while taking into account colliders sorrounding it.
    /// </summary>
    /// <param name="position">The position to move to.</param>
    public void MovePosition (Vector2 position) {
        Vector2 delta = new Vector2(transform.position.x, transform.position.y) - position;

        MoveByDelta(delta);
    }

    /// <summary>
    /// Moves the rigidbody by a certain delta while taking into account colliders sorrounding it.
    /// </summary>
    /// <param name="delta">The delta to move by.</param>
    public void MoveByDelta (Vector2 delta) {
        MoveByDeltaInteral(delta, false);
    }
    #endregion

    #region Internal Functions
    private void ApplyVelocity () {
        Vector2 delta = velocity * Time.fixedDeltaTime;
        if(delta.x > 0) {
            pVelDir.x = 1f;
        } else if(delta.x < 0) {
            pVelDir.x = -1f;
        }
        if(delta.y > 0) {
            pVelDir.y = 1f;
        } else if(delta.y < 0) {
            pVelDir.y = -1f;
        }
        delta += pVelDir * PhysicsPixel.inst.errorHandler;

        if(parentPlatform != null) {
            if(parentPlatform.canBeParentPlatform) {
                delta += parentPlatform.movementDelta;
            }
        }
        MoveByDeltaInteral(delta, true);
        transform.position -= (Vector3)(pVelDir * PhysicsPixel.inst.errorHandler);
    }

    private void MoveByDeltaInteral (Vector2 delta, bool limitVelocity) {
        Bounds2D bounds = GetBoundFromCollider();
        Bounds2D queryBounds = PhysicsPixel.inst.CalculateQueryBounds(bounds, delta);

        totalVolume = box.size.x * box.size.y;
        volume = totalVolume;
        subtractedVolume = totalVolume;

        QueryMinimizeApplyDelta(queryBounds, bounds, delta, limitVelocity);
        if(!isComplexCollider) {
            aabb = GetBoundFromCollider();
        } else {
            aabb = GetBoundFromCollider((Vector2)mobileChunk.mobileDataChunk.restrictedSize);
        }

        if(totalVolume > 0f) {
            submergedPercentage = 1f - (subtractedVolume / totalVolume);
        } else {
            submergedPercentage = 0f;
        }
    }
    #endregion

    #region Query and Delta Manipulations
    private void QueryMinimizeApplyDelta (Bounds2D queryBounds, Bounds2D bounds, Vector2 delta, bool limitVelocity) {
        Vector2 newDelta = delta;
        Bounds2D cBounds = bounds;

        // Figure out which tiles the query will seek
        Vector2Int queryTileMin = Vector2Int.FloorToInt((queryBounds.min - (Vector2)PhysicsPixel.inst.queryExtension));
        Vector2Int queryTileMax = Vector2Int.FloorToInt((queryBounds.max + (Vector2)PhysicsPixel.inst.queryExtension));

        // Sample the concerned tile size
        sampledCollisions.Clear();
        PhysicsPixel.inst.SampleCollisionInBounds(queryTileMin, queryTileMax, ref sampledCollisions, bounds, ref subtractedVolume);

        // Reduce the delta with the tile found if any
        if(sampledCollisions.Count > 0) {
            #region Reduce Delta Y
            // Figuring out the Y axis first, reduce the delta and apply it
            foreach(Bounds2D b in sampledCollisions) {
                // Reduce the delta
                newDelta.y = PhysicsPixel.inst.MinimizeDeltaY(newDelta.y, b, cBounds);
            }
            transform.position += newDelta.y * Vector3.up;

            // Recalculating current bounds
            cBounds = GetBoundFromCollider();
            #endregion

            #region Reduce Delta X
            // Figuring out the X axis after, reduce the delta and apply it
            foreach(Bounds2D b in sampledCollisions) {
                // Reduce the delta
                newDelta.x = PhysicsPixel.inst.MinimizeDeltaX(newDelta.x, b, cBounds);
            }
            transform.position += newDelta.x * Vector3.right;
            #endregion
        } else {
            transform.position += newDelta.y * Vector3.up;
            transform.position += newDelta.x * Vector3.right;
        }
        movementDelta += newDelta;

        #region Collision Detection
        // Calculating collisions that occured
        isCollidingDown = isCollidingDown || newDelta.y > delta.y;
        isCollidingUp = isCollidingUp || newDelta.y < delta.y;
        isCollidingLeft = isCollidingLeft || newDelta.x > delta.x;
        isCollidingRight = isCollidingRight || newDelta.x < delta.x;

        isCollidingWallDown = isCollidingWallDown || newDelta.y > delta.y;
        isCollidingWallUp = isCollidingWallUp || newDelta.y < delta.y;
        isCollidingWallLeft = isCollidingWallLeft || newDelta.x > delta.x;
        isCollidingWallRight = isCollidingWallRight || newDelta.x < delta.x;

        if(isCollidingDown) {
            velocity.x *= (1f - Time.fixedDeltaTime * defaultFloorFriction);
        }
        if(isCollidingLeft || isCollidingRight) {
            velocity.x *= (1f - Time.fixedDeltaTime * defaultWallFriction);
        }

        // Limit the velocity when a wall is it
        if(limitVelocity && !Mathf.Approximately(newDelta.y, delta.y))
            velocity.y = -velocity.y * terrainBounciness;
        if(limitVelocity && !Mathf.Approximately(newDelta.x, delta.x))
            velocity.x = -velocity.x * terrainBounciness;
        #endregion
    }
    #endregion

    #region Cliping
    void CheckForClipping () {
        float e = PhysicsPixel.inst.errorHandler;
        float e5 = 0.03125f;

        #region Left
        if(lastVel.x < 0f && isCollidingLeft) {
            Vector2 point0 = new Vector2(
                transform.position.x + box.offset.x - (box.size.x * 0.5f) - e5,
                transform.position.y + box.offset.y - (box.size.y * 0.5f) + e
            );
            Vector2 point1 = new Vector2(
                transform.position.x + box.offset.x - (box.size.x * 0.5f) - e5,
                transform.position.y + box.offset.y - (box.size.y * 0.5f) + clipAmout
            );
            bool solidClip = PhysicsPixel.inst.BoundsCast(new Bounds2D(point0, point1));
            bool solidPoint = PhysicsPixel.inst.IsPointSolid(point1 + Vector2.up * PhysicsPixel.inst.errorHandler);

            if(solidClip && !solidPoint) {
                if(PhysicsPixel.inst.AxisAlignedRaycast(point1, PhysicsPixel.Axis.Down, clipAmout, out Vector2 point)) {
                    float offset = clipAmout - (point1.y - point.y) + 0.03125f;
                    bool noFreeSpace = PhysicsPixel.inst.BoundsCast(new Bounds2D(point0 + Vector2.up * offset, point1 + Vector2.up * (offset + box.size.y - e)));
                    if(!noFreeSpace) {
                        transform.position += Vector3.up * offset;
                        velocity = lastVel;
                    }
                }
            }
        }
        #endregion

        #region Right
        if(lastVel.x > 0f && isCollidingRight) {
            Vector2 point0 = new Vector2(
                transform.position.x + box.offset.x + (box.size.x * 0.5f) + e5,
                transform.position.y + box.offset.y - (box.size.y * 0.5f) + e
            );
            Vector2 point1 = new Vector2(
                transform.position.x + box.offset.x + (box.size.x * 0.5f) + e5,
                transform.position.y + box.offset.y - (box.size.y * 0.5f) + clipAmout
            );
            bool solidClip = PhysicsPixel.inst.BoundsCast(new Bounds2D(point0, point1));
            bool solidPoint = PhysicsPixel.inst.IsPointSolid(point1 + Vector2.up * PhysicsPixel.inst.errorHandler);

            if(solidClip && !solidPoint) {
                if(PhysicsPixel.inst.AxisAlignedRaycast(point1, PhysicsPixel.Axis.Down, clipAmout, out Vector2 point)) {
                    float offset = clipAmout - (point1.y - point.y) + 0.03125f;
                    bool noFreeSpace = PhysicsPixel.inst.BoundsCast(new Bounds2D(point0 + Vector2.up * offset, point1 + Vector2.up * (offset + box.size.y - e)));
                    if(!noFreeSpace) {
                        transform.position += Vector3.up * offset;
                        velocity = lastVel;
                    }
                }
            }
        }
        #endregion

        #region Top Right
        if(lastVel.y > 5f && isCollidingUp) {
            Vector2 point0 = new Vector2(
                transform.position.x + box.offset.x + (box.size.x * 0.5f),
                transform.position.y + box.offset.y + (box.size.y * 0.5f)
            );
            Vector2 point1 = new Vector2(
                transform.position.x + box.offset.x + (box.size.x * 0.5f) - clipAmout,
                transform.position.y + box.offset.y + (box.size.y * 0.5f) + e5
            );
            bool solidClip = PhysicsPixel.inst.BoundsCast(new Bounds2D(point0, point1));
            bool solidPoint = PhysicsPixel.inst.IsPointSolid(point1 + Vector2.left * PhysicsPixel.inst.errorHandler);
            if(solidClip && !solidPoint) {
                if(PhysicsPixel.inst.AxisAlignedRaycast(point1, PhysicsPixel.Axis.Right, clipAmout, out Vector2 point)) {
                    float offset = clipAmout - (point.x - point1.x);
                    bool noFreeSpace = PhysicsPixel.inst.BoundsCast(new Bounds2D(point0 + Vector2.left * offset, point1 + Vector2.left * (offset + box.size.x - e)));
                    if(!noFreeSpace) {
                        transform.position += Vector3.left * offset;
                        velocity = lastVel;
                    }
                }
            }
        }
        #endregion

        #region Top Left
        if(lastVel.y > 5f && isCollidingUp) {
            Vector2 point0 = new Vector2(
                transform.position.x + box.offset.x - (box.size.x * 0.5f),
                transform.position.y + box.offset.y + (box.size.y * 0.5f)
            );
            Vector2 point1 = new Vector2(
                transform.position.x + box.offset.x - (box.size.x * 0.5f) + clipAmout,
                transform.position.y + box.offset.y + (box.size.y * 0.5f) + e5
            );
            bool solidClip = PhysicsPixel.inst.BoundsCast(new Bounds2D(point0, point1));
            bool solidPoint = PhysicsPixel.inst.IsPointSolid(point1 + Vector2.right * PhysicsPixel.inst.errorHandler);
            if(solidClip && !solidPoint) {
                if(PhysicsPixel.inst.AxisAlignedRaycast(point1, PhysicsPixel.Axis.Left, clipAmout, out Vector2 point)) {
                    float offset = clipAmout - (point1.x - point.x);
                    bool noFreeSpace = PhysicsPixel.inst.BoundsCast(new Bounds2D(point0 + Vector2.right * offset, point1 + Vector2.right * (offset + box.size.x - e)));
                    if(!noFreeSpace) {
                        transform.position += Vector3.right * offset;
                        velocity = lastVel;
                    }
                }
            }
        }
        #endregion
    }
    #endregion


    #region Parent Platform Utils
    public void SetParentPlatform (RigidbodyPixel pp, bool fullyConnected) {
        if(pp.canBeParentPlatform) {
            isParentPlaformConn = fullyConnected;
            parentPlatform = pp;
        }
    }

    public bool IsPrevParentingFullyConn () {
        if(previousParentPlatform != null) {
            return isPrevParentPlaformConn;
        }
        return false;
    }

    public bool IsParented () {
        return parentPlatform != null;
    }

    public Vector3 GetParentPosition () {
        if(parentPlatform.aligmentObject != null) {
            return new Vector3(parentPlatform.transform.position.x, parentPlatform.transform.position.y, parentPlatform.aligmentObject.position.z);
        } else {
            return parentPlatform.transform.position;
        }
    }

    public Vector3 GetPosition () {
        if(aligmentObject != null) {
            return new Vector3(transform.position.x, transform.position.y, aligmentObject.position.z);
        } else {
            return parentPlatform.transform.position;
        }
    }
    #endregion

    #region Simple Math Utils
    private bool IsRangeOverlapping (float min1, float max1, float min2, float max2) {
        return !(max1 <= min2 || min1 >= max2);
    }

    private float Min (float a, float b) {
        return a < b ? a : b;
    }

    private float Max (float a, float b) {
        return a > b ? a : b;
    }

    public Bounds2D GetBoundFromCollider () {
        return new Bounds2D((Vector2)transform.position - box.size * 0.5f + box.offset, (Vector2)transform.position + box.size * 0.5f + box.offset);
    }

    public Bounds2D GetBoundFromCollider (Vector2 trueSize) {
        return new Bounds2D(transform.position, (Vector2)transform.position + trueSize);
    }

    public Bounds2D GetBoundFromColliderDelta (Vector2 previsionDelta) {
        return new Bounds2D(
            new Vector2(transform.position.x + previsionDelta.x, transform.position.y + previsionDelta.y) - box.size * 0.5f + box.offset,
            new Vector2(transform.position.x + previsionDelta.x, transform.position.y + previsionDelta.y) + box.size * 0.5f + box.offset);
    }
    #endregion
}
