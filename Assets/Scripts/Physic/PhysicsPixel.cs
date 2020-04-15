using System.Collections.Generic;
using UnityEngine;

public class PhysicsPixel : MonoBehaviour {

    #region Header
    public static PhysicsPixel inst;

    [Header("General Parameters")]
    public float errorHandler = 0.00001f;
    public Vector3 queryExtension = Vector2.one * 0.5f;
    public float broadphaseMaxInterval = 0.25f;
    public Vector2 genericGravityForce = Vector2.down * -50f;

    [Header("Parenting Parameters")]
    [Range(0.15f, 1f)]
    public float posCorrectPercent = 0.2f;
    [Range(0.001f, 0.15f)]
    public float posCorrectSlop = 0.1f;
    public float parentingGap = 0.0625f;
    [Range(0f, 1f)]
    public float sideClipThreshold = 0.125f;

    [Header("Fluid Parameters")]
    public BaseTileAsset waterBackground;
    public float fluidDrag = 1f;
    public float fluidMassPerUnitDensity = 1f;

    
    

    [HideInInspector] public List<RigidbodyPixel> rbs;
    Queue<RigidbodyPixel> disabledRbs;
    
    private List<Bounds2D> sampledCollisions;
    List<Manifold> manifolds;
    Queue<Manifold> unusedManifold;
    #endregion

    #region MonoBehaviours
    private void Awake () {
        inst = this;
        rbs = new List<RigidbodyPixel>();
        disabledRbs = new Queue<RigidbodyPixel>();

        manifolds = new List<Manifold>();
        unusedManifold = new Queue<Manifold>();
        sampledCollisions = new List<Bounds2D>();
    }

    private void FixedUpdate () {
        // Applies force from components attached on RigibodyPixels
        ApplyForceComponents();
        
        SolveAllManifolds(true);

        GetAllWeakInteraction();
        ManageAllTerrainCollisions();

        ClearManifold();
        GetAllInteractions(false);
        SolveAllManifolds(false);

        ReanableAllRigibodies();
    }
    #endregion


    #region General Utility Functions
    /// <summary>
    /// Verifies if the tile at the given coordinates has any collision boxs and
    /// outputs the BaseTileAsset associated to it if any.
    /// </summary>
    /// <param name="tileX">The X coordinate of the tile to test</param>
    /// <param name="tileY">The Y coordinate of the tile to test</param>
    /// <param name="bta">The BaseTileAsset of the tile found if any</param>
    /// <returns></returns>
    public bool IsTileSolidAt (int tileX, int tileY, out BaseTileAsset bta, MobileDataChunk mdc = null) {

        if(TerrainManager.inst.GetGlobalIDAt(tileX, tileY, TerrainLayers.Ground, out int globalID, mdc)) {
            if(globalID != 0) {
                bta = TerrainManager.inst.tiles.GetTileAssetFromGlobalID(globalID);
                if(!bta.hasCollision) {
                    return false;
                }
                if(bta.collisionBoxes.Length == 0) {
                    return false;
                }
                return true;
            }
        }

        bta = null;
        return false;
    }

    /// <summary>
    /// Verifies if the given bound overlaps the collision of a tile, only if that said tile
    /// is solid.
    /// </summary>
    /// <param name="tileX">The X coordinate of the tile to test</param>
    /// <param name="tileY">The Y coordinate of the tile to test</param>
    /// <param name="bounds">The bounds that will be tested against the tile</param>
    /// <returns></returns>
    public bool BoundsIntersectTile (int tileX, int tileY, Bounds2D bounds) {
        // Seek tiles
        bool isTileSolid = IsTileSolidAt(tileX, tileY, out BaseTileAsset bta);
        if(!isTileSolid) return false;

        // Check for overlaps
        for(int b = 0; b < bta.collisionBoxes.Length; b++) {
            if(BoundsIntersectTileBounds(bta.collisionBoxes[b], tileX, tileY, bounds))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Verifies if the given ray hits the collision of a tile, only if that said tile
    /// is solid.
    /// </summary>
    /// <param name="tileX">The X coordinate of the tile to test</param>
    /// <param name="tileY">The Y coordinate of the tile to test</param>
    /// <param name="ray">The ray that will be tested against the tile</param>
    /// <returns></returns>
    public bool RayIntersectTile (int tileX, int tileY, Ray2D ray, out float distance) {
        // Seek tiles
        distance = Mathf.Infinity;
        bool isTileSolid = IsTileSolidAt(tileX, tileY, out BaseTileAsset bta);
        if(!isTileSolid)
            return false;

        Vector2 dirInv = new Vector2(1f/ray.direction.x, 1f/ray.direction.y);

        // Check for overlaps
        bool hasCollided = false;
        for(int b = 0; b < bta.collisionBoxes.Length; b++) {
            if(RayIntersectTileBounds(bta.collisionBoxes[b], tileX, tileY, ray.origin, dirInv, out float d)) {
                hasCollided = true;
                distance = Mathf.Min(distance, d);
            }
        }

        return hasCollided;
    }

    /// <summary>
    /// Casts the terrain to see if the given bounds is overlapping it
    /// </summary>
    /// <param name="bounds">The bounds to attempt to overlap on the terrain</param>
    /// <returns></returns>
    public bool BoundsCast (Bounds2D bounds) {
        // Figure out which tiles the query will seek
        Vector2Int queryTileMin = Vector2Int.FloorToInt(bounds.min);
        Vector2Int queryTileMax = Vector2Int.FloorToInt(bounds.max);
        bounds.min += new Vector2(errorHandler, errorHandler);
        bounds.max -= new Vector2(errorHandler, errorHandler);

        for(int tileX = queryTileMin.x; tileX <= queryTileMax.x; tileX++) {
            for(int tileY = queryTileMin.y; tileY <= queryTileMax.y; tileY++) {
                if(BoundsIntersectTile(tileX, tileY, bounds)) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Raycasts the terrain from a given ray, returns false if it didn't hit anything in the given range and returns true if did.
    /// </summary>
    /// <param name="ray">The ray (direction and origin) of the raycast</param>
    /// <param name="maxTileDistance">The max distance to raycast to before returning false</param>
    /// <param name="hitPoint">The point hit if any</param>
    /// <returns></returns>
    public bool RaycastTerrain (Ray2D ray, float maxTileDistance, out Vector2 hitPoint) {

        // Voxel traversal preparation
        Vector2 pos0 = new Vector2(ray.origin.x, ray.origin.y);
        Vector2 pos1 = pos0;
        Vector2Int step = new Vector2Int(
            (int)Mathf.Sign(ray.direction.x),
            (int)Mathf.Sign(ray.direction.y)
        );
        Vector2 tMax = new Vector2(
            IntBound(pos0.x, ray.direction.x),
            IntBound(pos0.y, ray.direction.y)
        );
        Vector2 tDelta = new Vector2(
            (ray.direction.x != 0) ? (1f / ray.direction.x * step.x) : maxTileDistance,
            (ray.direction.y != 0) ? (1f / ray.direction.y * step.y) : maxTileDistance
        );
        if(ray.direction.x == 0 && ray.direction.y == 0) {
            hitPoint = ray.origin;
            return false;
        }

        // Execute a 2D voxel traversal routine to find solid tile
        int limtCounter = (int)(maxTileDistance * Mathf.Sqrt(2f)) * 2;
        for(int i = limtCounter; i >= 0; i--) {
            Vector2Int tilePos = Vector2Int.FloorToInt(pos0);

            // Intersection calculation
            if(RayIntersectTile(tilePos.x, tilePos.y, ray, out float distance)) {
                if(distance > maxTileDistance) {
                    break;
                }

                hitPoint = ray.origin + ray.direction * distance;
                return true;
            }

            // Voxel traversal calculations
            if(tMax.x < tMax.y) {
                pos0.x += step.x;
                tMax.x += tDelta.x;
            } else {
                pos0.y += step.y;
                tMax.y += tDelta.y;
            }
            if(Vector2.Distance(pos0, pos1) > maxTileDistance) {
                break;
            }
        }

        hitPoint = ray.origin;
        return false;
    }

    /// <summary>
    /// Raycats the terrain in a specified axis, returns false if it didn't hit anything in the given range and returns true if did. This methode is more optimized than the Raycast one.
    /// </summary>
    /// <param name="origin">The staring point of the raycast</param>
    /// <param name="axis">To axis to follow</param>
    /// <param name="hitPoint">The point hit if any</param>
    /// <returns></returns>
    public bool AxisAlignedRaycast (Vector2 origin, Axis axis, float maxDistance, out Vector2 hitPoint) {
        Vector2Int initTilePos = Vector2Int.FloorToInt(origin);
        int maxIterations = Mathf.CeilToInt((maxDistance + 1));
        Vector2Int direction = new Vector2Int(
            (axis == Axis.Left) ? -1 : (axis == Axis.Right) ? 1 : 0,
            (axis == Axis.Down) ? -1 : (axis == Axis.Up) ? 1 : 0
        );

        hitPoint = Vector2.zero;

        Vector2Int tilePos = new Vector2Int();
        for(int i = 0; i < maxIterations; i++) {
            tilePos.Set(initTilePos.x + i * direction.x, initTilePos.y + i * direction.y);
            if(IsTileSolidAt(tilePos.x, tilePos.y, out BaseTileAsset bta)) {
                for(int b = 0; b < bta.collisionBoxes.Length; b++) {
                    Bounds2D bnds = GetTileBound(bta.collisionBoxes[b], tilePos.x, tilePos.y);

                    if(axis == Axis.Left || axis == Axis.Right) {
                        if(!IsPointInRange(bnds.min.y, bnds.max.y, origin.y)) {
                            continue;
                        }
                        if(axis == Axis.Left) {
                            float distance = origin.x - bnds.max.x;
                            if(distance < maxDistance) {
                                hitPoint = origin + Vector2.left * distance;
                                return true;
                            } else {
                                break;
                            }
                        } else {
                            float distance = bnds.min.x - origin.x;
                            if(distance < maxDistance) {
                                hitPoint = origin + Vector2.right * distance;
                                return true;
                            } else {
                                break;
                            }
                        }
                    } else {
                        if(!IsPointInRange(bnds.min.x, bnds.max.x, origin.x)) {
                            continue;
                        }
                        if(axis == Axis.Down) {
                            float distance = origin.y - bnds.max.y;
                            if(distance < maxDistance) {
                                hitPoint = origin + Vector2.down * distance;
                                return true;
                            } else {
                                break;
                            }
                        } else {
                            float distance = bnds.min.y - origin.y;
                            if(distance < maxDistance) {
                                hitPoint = origin + Vector2.up * distance;
                                return true;
                            } else {
                                break;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    public bool IsPointSolid (Vector2 point) {
        Vector2Int tile = Vector2Int.FloorToInt(point);

        // Seek tiles
        bool tileFound = TerrainManager.inst.GetGlobalIDAt(tile.x, tile.y, TerrainLayers.Ground, out int globalID, null);
        if(!tileFound || globalID == 0) {
            return false;
        }

        // Test asset
        BaseTileAsset bta = TerrainManager.inst.tiles.GetTileAssetFromGlobalID(TerrainManager.inst.GetGlobalIDAt(tile.x, tile.y, TerrainLayers.Ground, null));
        if(!bta.hasCollision) {
            return false;
        }
        if(bta.collisionBoxes.Length == 0) {
            return false;
        }
        for(int i = 0; i < bta.collisionBoxes.Length; i++) {
            if(bta.collisionBoxes[i].Overlaps(point - tile - (Vector2.one * 0.5f))) {
                return true;
            }
        }
        return false;
    }
    #endregion


    #region Solid Entity Collision Management

    // Manage the pooling of manifold object
    #region Manifold Object Pooling
    void ClearManifold () {
        foreach(Manifold m in manifolds) {
            unusedManifold.Enqueue(m);
        }
        manifolds.Clear();
    }
    
    Manifold GetNewManifold () {
        if(unusedManifold.Count > 0) {
            return unusedManifold.Dequeue();
        } else {
            return new Manifold();
        }
    }
    #endregion
 
    void SolveAllManifolds (bool isFirstRoutine) {
        for(int i = 0; i < manifolds.Count; i++) {
            manifolds[i].A.isInsideComplexObject = null;
            manifolds[i].B.isInsideComplexObject = null;
        }

        for(int i = 0; i < manifolds.Count; i++) {
            Manifold m = manifolds[i];

            if(isFirstRoutine) {
                if(!m.hadCollision && !m.anyBodyMoved) {
                    continue;
                } else {
                    if(!m.properComplexInteraction) {
                        m.doSolveManifold = GetManifold(m.A, m.B, ref m, isFirstRoutine, true);
                    } else {
                        m.doSolveManifold = GetManifold(m.A, m.B, ref m, isFirstRoutine, false);
                    }
                }
            }

            if(m.doSolveManifold && !(m.A.disableForAFrame || m.B.disableForAFrame)) {
                if(m.properComplexInteraction) {
                    SolveComplexCollision(m, isFirstRoutine);
                } else {
                    SolveCollision(m, isFirstRoutine);
                    PositionalCorrection(m);
                }
            }

            if(!isFirstRoutine) {
                DrawBounds(m.A.aabb, Color.red);
                DrawBounds(m.B.aabb, Color.red);
            }
        }
    }

    void GetAllInteractions (bool isFirstRoutine) {
        float inter = broadphaseMaxInterval;
        for(int i = 0; i < rbs.Count; i++) {
            for(int j = i + 1; j < rbs.Count; j++) {
                if(!rbs[i].gameObject.activeSelf || !rbs[j].gameObject.activeSelf) {
                    continue;
                }
                if((rbs[i].collidesOnlyWithTerrain || rbs[j].collidesOnlyWithTerrain)/* && !(rbs[i].interactsWithComplexCollider || rbs[j].interactsWithComplexCollider)*/) {
                    continue;
                }
                if(rbs[i].inverseMass == 0 && rbs[j].inverseMass == 0) {
                    continue;
                }
                if(rbs[i].disableForAFrame || rbs[j].disableForAFrame) {
                    continue;
                }

                //Broadphase Test
                if(!IsRangeOverlapping(rbs[i].aabb.min.x - inter, rbs[i].aabb.max.x + inter, rbs[j].aabb.min.x - inter, rbs[j].aabb.max.x + inter)) {
                    continue;
                }
                if(!IsRangeOverlapping(rbs[i].aabb.min.y - inter, rbs[i].aabb.max.y + inter, rbs[j].aabb.min.y - inter, rbs[j].aabb.max.y + inter)) {
                    continue;
                }

                Manifold manif = GetNewManifold();
                manif.anyBodyMoved = false;
                manif.hadCollision = false;
                manif.properComplexInteraction = (rbs[i].isComplexCollider && rbs[j].interactsWithComplexCollider) || (rbs[j].isComplexCollider && rbs[i].interactsWithComplexCollider);
                if(!manif.properComplexInteraction) {
                    manif.doSolveManifold = GetManifold(rbs[i], rbs[j], ref manif, isFirstRoutine, true);
                } else {
                    manif.doSolveManifold = GetManifold(rbs[i], rbs[j], ref manif, isFirstRoutine, false);
                }
                manifolds.Add(manif);
            }
        }
    }

    // Generate the manifold needed to solve interactions
    #region Generate Manifold
    bool GetManifold (RigidbodyPixel A, RigidbodyPixel B, ref Manifold m, bool isFirstRountine, bool doApplyStates) {
        m.A = A;
        m.B = B;
        m.IsACrushed = Vector2Int.zero;
        m.IsBCrushed = Vector2Int.zero;

        if(m.A.disableForAFrame || m.B.disableForAFrame) {
            m.hadCollision = false;
            m.normal = Vector2.zero;
            m.penetration = 0f;
            m.doSolveManifold = false;
            return false;
        }


        // Vector from A to B
        Bounds2D abox = A.GetBoundFromCollider();
        Bounds2D bbox = B.GetBoundFromCollider();
        Vector2 n = bbox.center - abox.center;

        float a_extent = (abox.max.x - abox.min.x) * 0.5f; // Calculate half extents along x axis for each object
        float b_extent = (bbox.max.x - bbox.min.x) * 0.5f;
        float x_overlap = a_extent + b_extent - Mathf.Abs(n.x); // Calculate overlap on x axis

        // SAT test on x axis
        if(x_overlap > 0) {
            // Calculate half extents along x axis for each object
            a_extent = (abox.max.y - abox.min.y) * 0.5f;
            b_extent = (bbox.max.y - bbox.min.y) * 0.5f;

            // Calculate overlap on y axis
            float y_overlap = a_extent + b_extent - Mathf.Abs(n.y);

            // SAT test on y axis
            if(y_overlap > 0) {
                // Find out which axis is axis of least penetration
                if(x_overlap < y_overlap) {
                    // Point towards B knowing that n points from A to B
                    if(n.x < 0) {
                        if(doApplyStates) {
                            m.A.hadCollisionLeft = true;
                            m.B.hadCollisionRight = true;
                            if(m.B.velocity.x > 0 && m.A.isCollidingWallRight) {
                                m.IsACrushed.x = 1;
                            }
                            if(m.A.velocity.x < 0 && m.B.isCollidingWallLeft) {
                                m.IsBCrushed.x = -1;
                            }
                        }
                        m.normal = Vector2.left;
                    } else {
                        if(doApplyStates) {
                            m.A.hadCollisionRight = true;
                            m.B.hadCollisionLeft = true;
                            if(m.B.velocity.x < 0 && m.A.isCollidingWallLeft) {
                                m.IsACrushed.x = -1;
                            }
                            if(m.A.velocity.x > 0 && m.B.isCollidingWallRight) {
                                m.IsBCrushed.x = 1;
                            }
                        }
                        m.normal = Vector2.right;
                    }
                    m.penetration = x_overlap;
                    m.hadCollision = true;
                    return true;
                } else {
                    // Point toward B knowing that n points from A to B
                    if(n.y < 0) {
                        if(doApplyStates) {
                            m.A.hadCollisionDown = true;
                            m.B.hadCollisionUp = true;
                            if(m.B.velocity.y > 0 && m.A.isCollidingUp) {
                                m.IsACrushed.y = 1;
                            }
                            if(m.A.velocity.y < 0 && m.B.isCollidingDown) {
                                m.IsBCrushed.y = -1;
                            }
                        }
                        m.normal = Vector2.down;
                    } else {
                        if(doApplyStates) {
                            m.A.hadCollisionUp = true;
                            m.B.hadCollisionDown = true;
                            if(m.B.velocity.y < 0 && m.A.isCollidingDown) {
                                m.IsACrushed.y = -1;
                            }
                            if(m.A.velocity.y > 0 && m.B.isCollidingUp) {
                                m.IsBCrushed.y = 1;
                            }
                        }
                        m.normal = Vector2.up;
                    }
                    m.penetration = y_overlap;
                    m.hadCollision = true;
                    return true;
                }
            } else {
                if(Mathf.Abs(y_overlap) <= parentingGap && doApplyStates) {
                    if(n.y < 0) {
                        m.A.hadCollisionDown = true;
                        m.B.hadCollisionUp = true;
                        if(B.canBeParentPlatform && A.velocity.y < 0) {
                            A.SetParentPlatform(B, false);
                        }
                        m.hadCollision = true;
                    } else {
                        m.B.hadCollisionDown = true;
                        m.A.hadCollisionUp = true;
                        if(A.canBeParentPlatform && B.velocity.y < 0) {
                            B.SetParentPlatform(A, false);
                        }
                        m.hadCollision = true;
                    }
                }
                return false;
            }
        } else {
            if(Mathf.Abs(x_overlap) <= parentingGap && doApplyStates) {
                if(n.x < 0) {
                    m.A.hadCollisionLeft = true;
                    m.B.hadCollisionRight = true;
                } else {
                    m.A.hadCollisionRight = true;
                    m.B.hadCollisionLeft = true;
                }
                m.hadCollision = true;
            }
            return false;
        }
    }

    bool ExpendManifold (RigidbodyPixel A, RigidbodyPixel B, Bounds2D bA, Bounds2D bB, ref Manifold m, bool isAOnFullSolidGround, bool isAOnEnoughGround) {
        m.A = A;
        m.B = B;

        // Vector from A to B
        Vector2 n = bB.center - bA.center;

        float a_extent = (bA.max.x - bA.min.x) * 0.5f; // Calculate half extents along x axis for each object
        float b_extent = (bB.max.x - bB.min.x) * 0.5f;
        float x_overlap = a_extent + b_extent - Mathf.Abs(n.x); // Calculate overlap on x axis

        // SAT test on x axis
        if(x_overlap > 0) {
            // Calculate half extents along x axis for each object
            a_extent = (bA.max.y - bA.min.y) * 0.5f;
            b_extent = (bB.max.y - bB.min.y) * 0.5f;

            // Calculate overlap on y axis
            float y_overlap = a_extent + b_extent - Mathf.Abs(n.y);

            // SAT test on y axis
            if(y_overlap > 0) {
                // Find out which axis is axis of least penetration
                bool eliminateXClip = isAOnFullSolidGround && (x_overlap - y_overlap) > -sideClipThreshold;
                bool forceXClip = !isAOnEnoughGround && (x_overlap - y_overlap) < sideClipThreshold;
                if((x_overlap < y_overlap && !eliminateXClip) || forceXClip) {
                    // Point towards B knowing that n points from A to B
                    if(n.x < 0) {
                        m.A.hadCollisionLeft = true;
                        m.B.hadCollisionRight = true;
                        if(m.B.velocity.x >= 0 && m.A.isCollidingWallRight) {
                            m.IsACrushed.x = 1;
                        }
                        if(m.A.velocity.x <= 0 && m.B.isCollidingWallLeft) {
                            m.IsBCrushed.x = -1;
                        }
                        m.normal = Vector2.left;
                    } else {
                        m.A.hadCollisionRight = true;
                        m.B.hadCollisionLeft = true;
                        if(m.B.velocity.x <= 0 && m.A.isCollidingWallLeft) {
                            m.IsACrushed.x = -1;
                        }
                        if(m.A.velocity.x >= 0 && m.B.isCollidingWallRight) {
                            m.IsBCrushed.x = 1;
                        }
                        m.normal = Vector2.right;
                    }
                    m.penetration = x_overlap;
                    m.hadCollision = true;
                    return true;
                } else {
                    // Point toward B knowing that n points from A to B
                    if(n.y < 0) {
                        m.A.hadCollisionDown = true;
                        m.B.hadCollisionUp = true;
                        if(m.B.velocity.y >= 0 && m.A.isCollidingUp) {
                            m.IsACrushed.y = 1;
                        }
                        if(m.A.velocity.y <= 0 && m.B.isCollidingDown) {
                            m.IsBCrushed.y = -1;
                        }
                        m.normal = Vector2.down;
                    } else {
                        m.A.hadCollisionUp = true;
                        m.B.hadCollisionDown = true;
                        if(m.B.velocity.y <= 0 && m.A.isCollidingDown) {
                            m.IsACrushed.y = -1;
                        }
                        if(m.A.velocity.y >= 0 && m.B.isCollidingUp) {
                            m.IsBCrushed.y = 1;
                        }
                        m.normal = Vector2.up;
                    }
                    m.penetration = y_overlap;
                    m.hadCollision = true;
                    return true;
                }
            } else {
                if(Mathf.Abs(y_overlap) <= parentingGap) {
                    if(n.y < 0) {
                        m.A.hadCollisionDown = true;
                        m.B.hadCollisionUp = true;
                        if(B.canBeParentPlatform && A.velocity.y < 0) {
                            A.SetParentPlatform(B, false);
                        }
                        m.hadCollision = true;
                    } else {
                        m.B.hadCollisionDown = true;
                        m.A.hadCollisionUp = true;
                        if(A.canBeParentPlatform && B.velocity.y < 0) {
                            B.SetParentPlatform(A, false);
                        }
                        m.hadCollision = true;
                    }
                }
                return false;
            }
        } else {
            if(Mathf.Abs(x_overlap) <= parentingGap) {
                if(n.x < 0) {
                    m.A.hadCollisionLeft = true;
                    m.B.hadCollisionRight = true;
                } else {
                    m.A.hadCollisionRight = true;
                    m.B.hadCollisionLeft = true;
                }
                m.hadCollision = true;
            }
            return false;
        }
    }

    float GetFloorValue (Bounds2D bA, Bounds2D bB) {
        // Vector from A to B
        Vector2 n = bB.center - bA.center;

        float a_extent = (bA.max.x - bA.min.x) * 0.5f; // Calculate half extents along x axis for each object
        float b_extent = (bB.max.x - bB.min.x) * 0.5f;
        float x_overlap = a_extent + b_extent - Mathf.Abs(n.x); // Calculate overlap on x axis

        // SAT test on x axis
        if(x_overlap > 0) {
            // Calculate half extents along x axis for each object
            a_extent = (bA.max.y - bA.min.y) * 0.5f;
            b_extent = (bB.max.y - bB.min.y) * 0.5f;

            // Calculate overlap on y axis
            float y_overlap = a_extent + b_extent - Mathf.Abs(n.y);

            // SAT test on y axis
            if(y_overlap > 0) {
                // Find out which axis is axis of least penetration
                return x_overlap;
            } else {
                if(Mathf.Abs(y_overlap) <= parentingGap) {
                    return x_overlap;
                }
            }
        }
        return 0f;
    }
    #endregion

    // Solve interaction of an entity with another
    #region Simple Solve
    void SolveCollision (Manifold m, bool isFirstRountine) {
        Vector2 amul = Vector2.one;
        Vector2 bmul = Vector2.one;
        
        if(m.normal == Vector2.up) {
            m.B.SetParentPlatform(m.A, true);
            if(!isFirstRountine) {
                m.B.velocity.y = 0f;
            }
            if(m.B.IsPrevParentingFullyConn()) {
                amul.y = 0f;
                bmul.y = 0f;
            } else if(m.B.eliminatesPenetration) {
                bmul.y = 0f;
            }
        }
        if(m.normal == Vector2.down) {
            m.A.SetParentPlatform(m.B, true);
            if(!isFirstRountine) {
                m.A.velocity.y = 0f;
            }
            if(m.A.IsPrevParentingFullyConn()) {
                amul.y = 0f;
                bmul.y = 0f;
            } else if(m.A.eliminatesPenetration) {
                amul.y = 0f;
            }
        }

        // Calculate relative velocity
        Vector2 rv = m.B.velocity - m.A.velocity;

        // Calculate relative velocity in terms of the normal direction
        float velAlongNormal = Vector2.Dot(rv, m.normal);

        // Do not resolve if velocities are separating
        if(velAlongNormal > 0.0f) return;

        // Calculate restitution
        float e = Mathf.Min(m.A.bounciness, m.B.bounciness);

        // Calculate impulse scalar
        float j = -(1 + e) * velAlongNormal;
        j /= m.A.inverseMass + m.B.inverseMass;

        if(m.IsACrushed.x != 0 && m.IsBCrushed.x == 0) {
            m.B.velocity.x = 0f;
            amul.x = 0f;
            bmul.x = 0f;
        } else if(m.IsACrushed.x == 0 && m.IsBCrushed.x != 0) {
            m.A.velocity.x = 0f;
            amul.x = 0f;
            bmul.x = 0f;
        }

        if(m.IsACrushed.y != 0 && m.IsBCrushed.y == 0) {
            m.B.velocity.y = 0f;
            amul.y = 0f;
            bmul.y = 0f;
        } else if(m.IsACrushed.y == 0 && m.IsBCrushed.y != 0) {
            m.A.velocity.y = 0f;
            amul.y = 0f;
            bmul.y = 0f;
        }

        // Apply impulse
        Vector2 impulse = j * m.normal;
        m.A.velocity -= m.A.inverseMass * impulse * amul;
        m.B.velocity += m.B.inverseMass * impulse * bmul;
    }

    void PositionalCorrection (Manifold m) {
        Vector2 correctionA = Vector2.zero;
        Vector2 correctionB = Vector2.zero;

        if(m.IsACrushed.x == 0 && m.IsBCrushed.x == 0) {
            correctionA.x = -m.A.inverseMass * (Mathf.Max(m.penetration - posCorrectSlop, 0.0f) / (m.A.inverseMass + m.B.inverseMass)) * posCorrectPercent * m.normal.x;
            correctionB.x = m.B.inverseMass * (Mathf.Max(m.penetration - posCorrectSlop, 0.0f) / (m.A.inverseMass + m.B.inverseMass)) * posCorrectPercent * m.normal.x;
        } else if(m.IsACrushed.x != 0 && m.IsBCrushed.x == 0) {
            correctionA.x = 0f;
            correctionB.x = Mathf.Max(m.penetration - posCorrectSlop, 0.0f) * m.normal.x;
        } else if(m.IsACrushed.x == 0 && m.IsBCrushed.x != 0) {
            correctionB.x = 0f;
            correctionA.x = -Mathf.Max(m.penetration - posCorrectSlop, 0.0f) * m.normal.x;
        }

        if(m.IsACrushed.y == 0 && m.IsBCrushed.y == 0) {
            correctionA.y = -m.A.inverseMass * (Mathf.Max(m.penetration - posCorrectSlop, 0.0f) / (m.A.inverseMass + m.B.inverseMass)) * posCorrectPercent * m.normal.y;
            correctionB.y = m.B.inverseMass * (Mathf.Max(m.penetration - posCorrectSlop, 0.0f) / (m.A.inverseMass + m.B.inverseMass)) * posCorrectPercent * m.normal.y;
        } else if(m.IsACrushed.y != 0 && m.IsBCrushed.y == 0) {
            correctionA.y = 0f;
            correctionB.y = Mathf.Max(m.penetration - posCorrectSlop, 0.0f) * m.normal.y;
        } else if(m.IsACrushed.y == 0 && m.IsBCrushed.y != 0) {
            correctionB.y = 0f;
            correctionA.y = -Mathf.Max(m.penetration - posCorrectSlop, 0.0f) * m.normal.y;
        } else {
            if(m.A.transform.position.y < m.B.transform.position.y) {
                correctionA.y = 0f;
                correctionB.y = Mathf.Max(m.penetration - posCorrectSlop, 0.0f) * m.normal.y;
            } else {
                correctionB.y = 0f;
                correctionA.y = -Mathf.Max(m.penetration - posCorrectSlop, 0.0f) * m.normal.y;
            }
        }

        m.A.MoveByDelta(correctionA);
        m.B.MoveByDelta(correctionB);

        if(correctionA.x != 0f || correctionA.y != 0f) {
            if(!m.A.isComplexCollider) {
                m.A.aabb = m.A.GetBoundFromCollider();
            } else {
                m.A.aabb = m.A.GetBoundFromCollider((Vector2)m.A.mobileChunk.mobileDataChunk.restrictedSize);
            }
            
            m.anyBodyMoved = true;
        }
        if(correctionB.x != 0f || correctionB.y != 0f) {
            if(!m.B.isComplexCollider) {
                m.B.aabb = m.B.GetBoundFromCollider();
            } else {
                m.B.aabb = m.B.GetBoundFromCollider((Vector2)m.B.mobileChunk.mobileDataChunk.restrictedSize);
            }

            m.anyBodyMoved = true;
        }
    }
    #endregion

    // Solve interaction of an entity with a mobile chunk
    #region Complex Solve
    void SolveComplexCollision (Manifold m, bool isFirstRountine) {
        RigidbodyPixel pl = (m.A.interactsWithComplexCollider) ? m.A : m.B;
        RigidbodyPixel cm = (m.A.isComplexCollider) ? m.A : m.B;
        pl.isInsideComplexObject = cm;

        Bounds2D playerBounds = pl.aabb;
        Vector2Int queryTileMin = Vector2Int.FloorToInt((playerBounds.min - (Vector2)cm.transform.position));
        Vector2Int queryTileMax = Vector2Int.FloorToInt((playerBounds.max - (Vector2)cm.transform.position));

        sampledCollisions.Clear();
        SampleCollisionInBounds(queryTileMin, queryTileMax, ref sampledCollisions, playerBounds, cm.mobileChunk.mobileDataChunk);
        
        Manifold tempManifold = new Manifold();
        tempManifold.IsACrushed = Vector2Int.zero;
        tempManifold.IsBCrushed = Vector2Int.zero;

        float floorValue = 0f;
        foreach(Bounds2D b in sampledCollisions) {
            Bounds2D boxBounds = b;
            boxBounds.Move(cm.transform.position);

            floorValue += GetFloorValue(pl.aabb, boxBounds);
        }

        foreach(Bounds2D b in sampledCollisions) {
            Bounds2D boxBounds = b;
            boxBounds.Move(cm.transform.position);

            bool collided = ExpendManifold(pl, cm, pl.aabb, boxBounds, ref tempManifold,
                floorValue >= (playerBounds.size.x - errorHandler * 2f),
                floorValue >= (2f * sideClipThreshold)
            );
            DrawBounds(boxBounds, Color.magenta);

            SolveCollision(tempManifold, isFirstRountine);
            PositionalCorrection(tempManifold);
        }
    }
    #endregion

    // Utility functions
    #region Utils
    void ApplyForceComponents () {
        for(int i = 0; i < rbs.Count; i++) {
            if(!rbs[i].gameObject.activeSelf) {
                continue;
            }

            if(rbs[i].disableForAFrame) {
                disabledRbs.Enqueue(rbs[i]);
                continue;
            }
            rbs[i].ApplyBuoyancy();
            rbs[i].ApplyForces();
        }
    }

    void ReanableAllRigibodies () {
        while(disabledRbs.Count > 0) {
            RigidbodyPixel rb = disabledRbs.Dequeue();
            rb.disableForAFrame = false;
        }
    }
    #endregion
    #endregion

    #region Weak Entity Collision Management
    void GetAllWeakInteraction () {
        float inter = broadphaseMaxInterval;
        for(int i = 0; i < rbs.Count; i++) {
            for(int j = i + 1; j < rbs.Count; j++) {
                if(!rbs[i].gameObject.activeSelf || !rbs[j].gameObject.activeSelf) {
                    continue;
                }
                if(!rbs[i].weakPushCandidate || !rbs[j].weakPushCandidate) {
                    continue;
                }
                if(rbs[i].inverseMass == 0 && rbs[j].inverseMass == 0) {
                    continue;
                }
                if(rbs[i].disableForAFrame || rbs[j].disableForAFrame) {
                    continue;
                }

                //Broadphase Test
                if(!IsRangeOverlapping(rbs[i].aabb.min.x - inter, rbs[i].aabb.max.x + inter, rbs[j].aabb.min.x - inter, rbs[j].aabb.max.x + inter)) {
                    continue;
                }
                if(!IsRangeOverlapping(rbs[i].aabb.min.y - inter, rbs[i].aabb.max.y + inter, rbs[j].aabb.min.y - inter, rbs[j].aabb.max.y + inter)) {
                    continue;
                }

                if(GetWeakManifold(rbs[i].aabb, rbs[j].aabb, out Vector2 normal, out float penetration)) {
                    SolveWeakManifold(rbs[i], rbs[j], normal, penetration);
                }
            }
        }
    }

    void SolveWeakManifold (RigidbodyPixel a, RigidbodyPixel b, Vector2 normal, float penetration) {
        // Calculate relative velocity
        Vector2 rv = b.velocity - a.velocity;

        // Calculate relative velocity in terms of the normal direction
        float velAlongNormal = Vector2.Dot(rv, normal);

        // Do not resolve if velocities are separating
        if(velAlongNormal > 0.0f)
            return;

        // Calculate restitution
        float e = 0.5f;

        // Calculate impulse scalar
        float j = -(1 + e) * velAlongNormal;
        j /= a.inverseMass + b.inverseMass;

        // Apply impulse
        Vector2 impulse = j * normal;
        a.velocity -= a.inverseMass * impulse * 0.5f;
        b.velocity += b.inverseMass * impulse * 0.5f;
    }

    bool GetWeakManifold (Bounds2D abox, Bounds2D bbox, out Vector2 normal, out float penetration) {
        Vector2 n = bbox.center - abox.center;

        float a_extent = (abox.max.x - abox.min.x) * 0.5f; // Calculate half extents along x axis for each object
        float b_extent = (bbox.max.x - bbox.min.x) * 0.5f;
        float x_overlap = a_extent + b_extent - Mathf.Abs(n.x); // Calculate overlap on x axis

        normal = Vector2.zero;
        penetration = 0f;

        // SAT test on x axis
        if(x_overlap > 0) {
            // Calculate half extents along x axis for each object
            a_extent = (abox.max.y - abox.min.y) * 0.5f;
            b_extent = (bbox.max.y - bbox.min.y) * 0.5f;

            // Calculate overlap on y axis
            float y_overlap = a_extent + b_extent - Mathf.Abs(n.y);

            // SAT test on y axis
            if(y_overlap > 0) {
                // Find out which axis is axis of least penetration
                if(x_overlap < y_overlap) {
                    // Point towards B knowing that n points from A to B
                    if(n.x < 0) {
                        normal = Vector2.left;
                    } else {
                        normal = Vector2.right;
                    }
                    penetration = x_overlap;
                    return true;
                } else {
                    // Point toward B knowing that n points from A to B
                    if(n.y < 0) {
                        normal = Vector2.down;
                    } else {
                        normal = Vector2.up;
                    }
                    penetration = y_overlap;
                    return true;
                }
            } else {
                return false;
            }
        } else {
            return false;
        }
    }
    #endregion

    #region Terrain-Entity Collision Management
    void ManageAllTerrainCollisions () {
        foreach(RigidbodyPixel rb in rbs) {
            if(!rb.IsParented() && !rb.disableForAFrame && rb.gameObject.activeSelf && !rb.secondExecutionOrder) {
                rb.SimulateFixedUpdate();
            }
        }
        foreach(RigidbodyPixel rb in rbs) {
            if(!rb.IsParented() && !rb.disableForAFrame && rb.gameObject.activeSelf && rb.secondExecutionOrder) {
                rb.SimulateFixedUpdate();
            }
        }
        foreach(RigidbodyPixel rb in rbs) {
            if(rb.IsParented() && !rb.disableForAFrame && rb.gameObject.activeSelf && !rb.secondExecutionOrder) {
                rb.SimulateFixedUpdate();
            }
        }
        foreach(RigidbodyPixel rb in rbs) {
            if(rb.IsParented() && !rb.disableForAFrame && rb.gameObject.activeSelf && rb.secondExecutionOrder) {
                rb.SimulateFixedUpdate();
            }
        }
    }
    #endregion


    #region General Math Utils
    public bool BoundsIntersectTileBounds (Bounds2D tileBounds, int tileX, int tileY, Bounds2D collidingBounds) {
        Bounds2D tB = tileBounds;
        tB.PositionToTile(tileX, tileY);
        return collidingBounds.Overlaps(tB);
    }

    public bool RayIntersectTileBounds (Bounds2D tileBounds, int tileX, int tileY, Vector2 rayOrigin, Vector2 rayInvDir, out float distance) {
        Bounds2D tB = tileBounds;
        tB.PositionToTile(tileX, tileY);
        bool doIntersect = tB.IntersectRay(rayOrigin, rayInvDir, out float d);
        distance = d;
        return doIntersect;
    }

    public Bounds2D GetTileBound (Bounds2D tileBounds, int tileX, int tileY) {
        Bounds2D tB = tileBounds;
        tB.PositionToTile(tileX, tileY);
        return tB;
    }
    #endregion

    #region Rigibody Math Utils
    public float MinimizeDeltaX (float deltaX, Bounds2D collider, Bounds2D bounds) {
        // Bail out if not within the same Y plane.
        if(!IsRangeOverlapping(collider.min.y, collider.max.y, bounds.min.y, bounds.max.y)) {
            return deltaX;
        }

        //Reduce the delta to the smallest (ignoring the sign) colliding "axis".
        if(deltaX < 0 && bounds.min.x >= collider.max.x) {
            return Max(deltaX, collider.max.x - bounds.min.x + errorHandler);
        } else if(deltaX > 0 && bounds.max.x <= collider.min.x) {
            return Min(deltaX, collider.min.x - bounds.max.x - errorHandler);
        }

        return deltaX;
    }

    public float MinimizeDeltaY (float deltaY, Bounds2D collider, Bounds2D bounds) {
        // Bail out if not within the same X plane.
        if(!IsRangeOverlapping(collider.min.x, collider.max.x, bounds.min.x, bounds.max.x)) {
            return deltaY;
        }

        //Reduce the delta to the smallest (ignoring the sign) colliding "axis".
        if(deltaY < 0 && bounds.min.y >= collider.max.y) {
            return Max(deltaY, collider.max.y - bounds.min.y + errorHandler);
        } else if(deltaY > 0 && bounds.max.y <= collider.min.y) {
            return Min(deltaY, collider.min.y - bounds.max.y - errorHandler);
        }

        return deltaY;
    }

    public Bounds2D CalculateQueryBounds (Bounds2D bounds, Vector2 delta) {
        Bounds2D queryBounds = bounds;

        // Extends a bounds to get a query bounds to know which tile to check collision with.
        if(delta.x < 0) {
            queryBounds.min = new Vector2(queryBounds.min.x + delta.x, queryBounds.min.y);
        } else {
            queryBounds.max = new Vector2(queryBounds.max.x + delta.x, queryBounds.max.y);
        }
        if(delta.y < 0) {
            queryBounds.min = new Vector2(queryBounds.min.x, queryBounds.min.y + delta.y);
        } else {
            queryBounds.max = new Vector2(queryBounds.max.x, queryBounds.max.y + delta.y);
        }

        return queryBounds;
    }

    public void SampleCollisionInBounds (Vector2Int queryTileMin, Vector2Int queryTileMax, ref List<Bounds2D> sampledCollisions, Bounds2D colliderBounds, MobileDataChunk mdc = null) {
        for(int tileX = queryTileMin.x; tileX <= queryTileMax.x; tileX++) {
            for(int tileY = queryTileMin.y; tileY <= queryTileMax.y; tileY++) {
                if(mdc != null && IsTileInsideBound(tileX, tileY, colliderBounds)) {
                    continue;
                }
                if(IsTileSolidAt(tileX, tileY, out BaseTileAsset bta, mdc)) {
                    for(int b = 0; b < bta.collisionBoxes.Length; b++) {
                        sampledCollisions.Add(GetTileBound(bta.collisionBoxes[b], tileX, tileY));
                    }
                } else {
                    continue;
                }
            }
        }
    }

    public void SampleCollisionInBounds (Vector2Int queryTileMin, Vector2Int queryTileMax, ref List<Bounds2D> sampledCollisions, Bounds2D colliderBounds, ref float substractVolume) {
        for(int tileX = queryTileMin.x; tileX <= queryTileMax.x; tileX++) {
            for(int tileY = queryTileMin.y; tileY <= queryTileMax.y; tileY++) {
                if(IsTileSolidAt(tileX, tileY, out BaseTileAsset bta)) {
                    for(int b = 0; b < bta.collisionBoxes.Length; b++) {
                        sampledCollisions.Add(GetTileBound(bta.collisionBoxes[b], tileX, tileY));
                    }
                    continue;
                }

                //Do water calculations
                if(!IsTileTouchingBound(tileX, tileY, colliderBounds)) {
                    continue;
                }
                if(!TerrainManager.inst.GetGlobalIDBitmaskAt(tileX, tileY, TerrainLayers.WaterBackground, out int globalID, out int bitmask)) {
                    continue;
                }
                if(globalID != waterBackground.globalID) {
                    continue;
                }
                Vector2 area;
                if(((bitmask >> 1) & 1) == 0) {
                    area = colliderBounds.GetIntersectingArea(new Bounds2D(new Vector2(tileX, tileY), new Vector2(tileX + 1f, tileY + 0.75f)));
                } else {
                    area = colliderBounds.GetIntersectingArea(new Bounds2D(new Vector2(tileX, tileY), new Vector2(tileX + 1f, tileY + 1f)));
                }
                substractVolume -= (area.x * area.y);
            }
        }
    }
    #endregion


    #region Simple Math Utils
    private bool IsRangeOverlapping (float min1, float max1, float min2, float max2) {
        return max1 > min2 && min1 < max2;
    }

    private bool IsPointInRange (float min, float max, float point) {
        return point < max && point > min;
    }

    private float IntBound (float s, float ds) {
        // Find the smallest positive t such that s+t*ds is an integer.
        if(ds < 0) {
            return IntBound(-s, -ds);
        } else {
            s = Modulo(s, 1);
            // problem is now s+t*ds = 1
            return (1 - s) / ds;
        }
    }

    private float Modulo (float value, float modulus) {
        return (value % modulus + modulus) % modulus;
    }

    private int Modulo (int value, int modulus) {
        return (value % modulus + modulus) % modulus;
    }

    private float Min (float a, float b) {
        return a < b ? a : b;
    }

    private float Max (float a, float b) {
        return a > b ? a : b;
    }

    private bool BoundsIntersectWithoutTouch (Bounds2D a, Bounds2D b) {
        return IsRangeOverlapping(a.min.x, a.max.x, b.min.x, b.max.x) && IsRangeOverlapping(a.min.y, a.max.y, b.min.y, b.max.y);
    }

    private bool IsTileInsideBound (int tileX, int tileY, Bounds2D bounds) {
        return bounds.Overlaps(new Vector2(tileX       + errorHandler, tileY *     + errorHandler)) &&
               bounds.Overlaps(new Vector2((tileX + 1) - errorHandler, (tileY + 1) - errorHandler));
    }

    private bool IsTileTouchingBound (int tileX, int tileY, Bounds2D bounds) {
        return  IsRangeOverlapping(bounds.min.x, bounds.max.x, tileX, tileX + 1) &&
                IsRangeOverlapping(bounds.min.y, bounds.max.y, tileY, tileY + 1);
    }
    #endregion

    #region DataTypes and Enums
    public enum Axis {
        Up,
        Down,
        Right,
        Left
    }

    public class Manifold {
        public RigidbodyPixel A;
        public RigidbodyPixel B;
        public float penetration;
        public Vector2 normal;
        public Vector2Int IsACrushed;
        public Vector2Int IsBCrushed;
        public bool hadCollision;
        public bool doSolveManifold;
        public bool anyBodyMoved;
        public bool properComplexInteraction;
    };
    #endregion

    #region Debug
    public static void DrawBounds (Bounds bounds, Color color) {
        Debug.DrawLine(new Vector2(bounds.min.x, bounds.min.y), new Vector2(bounds.max.x, bounds.min.y), color);
        Debug.DrawLine(new Vector2(bounds.min.x, bounds.max.y), new Vector2(bounds.max.x, bounds.max.y), color);
        Debug.DrawLine(new Vector2(bounds.min.x, bounds.min.y), new Vector2(bounds.min.x, bounds.max.y), color);
        Debug.DrawLine(new Vector2(bounds.max.x, bounds.min.y), new Vector2(bounds.max.x, bounds.max.y), color);
    }

    public static void DrawBounds (Bounds2D bounds, Color color) {
        Debug.DrawLine(new Vector2(bounds.min.x, bounds.min.y), new Vector2(bounds.max.x, bounds.min.y), color);
        Debug.DrawLine(new Vector2(bounds.min.x, bounds.max.y), new Vector2(bounds.max.x, bounds.max.y), color);
        Debug.DrawLine(new Vector2(bounds.min.x, bounds.min.y), new Vector2(bounds.min.x, bounds.max.y), color);
        Debug.DrawLine(new Vector2(bounds.max.x, bounds.min.y), new Vector2(bounds.max.x, bounds.max.y), color);
    }
    #endregion
}
