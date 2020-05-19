using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeExp : MonoBehaviour { 

    public Camera cam;
    public LineRenderer lineRenderer;
    public Transform anchor;

    [Header("Rope Parameters")]
    public int segmentCount;
    public float targetChainSegmentLength = 0.333f;
    public float segmentMass = 0.5f;
    public float anchorMass = 16f;
    public float collRadius = 0.25f;

    [Header("Rope Physic Parameters")]
    public Vector2 gravity = new Vector2(0, -10f);
    public int simulationSteps = 4;
    public float chainAirDrag = 0.7f;
    public float anchorAirDrag = 0.1f;

    [Header("Spring Simulation Parameters")]
    public bool useWarmStart = false;
    public float springFrequency = 6000f;
    public float springDampRatio = 50f;
    public float jointMaxPosCorrection = 200f;
    public bool enablePosCorrection = false;

    List<RopeJoint> distanceJoints;
    List<MovingMass> movingMasses;
    Vector3[] positions;

    void Start () {
        GenerateRope();
    }

    private void Update () {
        if(Input.GetKey(KeyCode.C)) {
            pinPoint = cam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            Vector2 aimPoint = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (aimPoint - pinPoint).normalized;

            for(int i = 0; i < movingMasses.Count; i++) {
                movingMasses[i].position = pinPoint;
                movingMasses[i].prevPos = pinPoint;
                movingMasses[i].velocity = Vector2.zero;
            }

            movingMasses[movingMasses.Count - 1].velocity = dir * 50f;
        }
    }

    private void FixedUpdate () {
        PinRopeToMouse();
        Simulate();
        DrawRope();
    }

    Vector2 pinPoint = Vector2.zero;
    private void PinRopeToMouse () {
        pinPoint = cam.ScreenToWorldPoint(Input.mousePosition);
        //pinPoint = cam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));

        if(Input.GetMouseButton(0)) {
            for(int i = 0; i < movingMasses.Count; i++) {
                movingMasses[i].position = pinPoint;
                movingMasses[i].prevPos = pinPoint;
                movingMasses[i].velocity = Vector2.zero;
            }
        }
    }

    void GenerateRope () {
        distanceJoints = new List<RopeJoint>();
        movingMasses = new List<MovingMass>();
        positions = new Vector3[segmentCount];

        Vector2 initPos = transform.position;
        
        for(int i = 0; i < segmentCount; i++) {
            MovingMass mm;
            if(i == segmentCount - 1) {
                mm = new MovingMass(initPos, anchorMass, collRadius);
            } else {
                mm = new MovingMass(initPos, segmentMass, collRadius);
            }
            movingMasses.Add(mm);

            initPos += Vector2.down * targetChainSegmentLength;
        }

        for(int i = 1; i < segmentCount; i++) {
            distanceJoints.Add(new RopeJoint(this, movingMasses[i-1], movingMasses[i]));
        }
    }

    void Simulate () {
        float deltaTimeStep = Time.deltaTime / simulationSteps;
        float deltaTime = Time.deltaTime;

        for(int simstep = 0; simstep < simulationSteps; simstep++) {
            movingMasses[0].position = pinPoint;
            movingMasses[0].velocity = Vector2.zero;
            movingMasses[0].forces = Vector2.zero;
            for(int i = 0; i < movingMasses.Count; i++) {
                MovingMass mm = movingMasses[i];

                mm.AddForce(gravity.x * mm.mass, gravity.y * mm.mass);
                movingMasses[i].UpdatePosition(deltaTimeStep);
                mm.UpdateCollision();
            }
            for(int j = distanceJoints.Count - 1; j >= 0; j--) {
                distanceJoints[j].InitiateVelocityConstraint(Time.deltaTime);
            }
            for(int j = distanceJoints.Count - 1; j >= 0; j--) {
                distanceJoints[j].CalculateVelocityConstraint(Time.deltaTime);
            }
            for(int j = distanceJoints.Count - 1; j >= 0; j--) {
                distanceJoints[j].SolvePositionConstraints(Time.deltaTime);
            }
        }

        for(int i = 0; i < movingMasses.Count; i++) {
            MovingMass mm = movingMasses[i];

            if(i == movingMasses.Count - 1) {
                mm.velocity *= 1.0f / (1.0f + deltaTime * anchorAirDrag);
            } else {
                mm.velocity *= 1.0f / (1.0f + deltaTime * chainAirDrag);
            }

            mm.UpdateCollision();
        }
    }

    void DrawRope () {
        for(int i = 0; i < movingMasses.Count; i++) {
            positions[i] = movingMasses[i].position;
        }
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);

        Vector2 diff = movingMasses[movingMasses.Count - 2].position - movingMasses[movingMasses.Count - 1].position;
        anchor.eulerAngles = Vector3.forward * Mathf.Atan2(-diff.x, diff.y) * Mathf.Rad2Deg;
        anchor.transform.position = movingMasses[movingMasses.Count - 1].position;
    }
}

public class RopeJoint {
    public RopeExp rope;

    public MovingMass a;
    public MovingMass b;

    // Failed attempt at reading Box2D's variables
    const float b2_linearSlop = 0.005f;
    float m_u_x;
    float m_u_y;
    float m_u_mag;
    float m_impulse;
    float m_mass;
    float m_gamma;
    float m_bias;

    public RopeJoint (RopeExp rope, MovingMass a, MovingMass b) {
        this.rope = rope;
        this.a = a;
        this.b = b;
    }


    #region Update Springs
    public void InitiateVelocityConstraint (float deltaTime) {
        m_u_x = b.position.x - a.position.x;
        m_u_y = b.position.y - a.position.y;
        float length = Mathf.Sqrt(m_u_x * m_u_x + m_u_y * m_u_y);
        m_u_mag = length;
        if(length > b2_linearSlop) {
            m_u_x /= length;
            m_u_y /= length;
        } else {
            m_u_x = 0f;
            m_u_y = 0f;
        }

        float invMass = a.invMass + b.invMass;
        m_mass = invMass != 0.0f ? 1.0f / invMass : 0.0f;

        if(rope.springFrequency > 0.0f) {
            float distError = length - rope.targetChainSegmentLength;
            float omega = 2.0f * Mathf.PI * rope.springFrequency;   // Frequency
            float dampCo = 2.0f * m_mass * rope.springDampRatio * omega; // Damping coefficient
            float k = m_mass * omega * omega;                       // Spring stiffness
            m_gamma = deltaTime * (dampCo + deltaTime * k);         // gamma = 1 / (h * (d + h * k)), the extra factor of h in the denominator is since the lambda is an impulse, not a force
            m_gamma = m_gamma != 0.0f ? 1.0f / m_gamma : 0.0f;
            m_bias = distError * deltaTime * k * m_gamma;

            invMass += m_gamma;
            m_mass = invMass != 0.0f ? 1.0f / invMass : 0.0f;
        } else {
            // m_gamma = 0.0f;
            m_bias = 0.0f;
        }

        // Scale the impulse to support a variable time step.
        if(!rope.useWarmStart) {
            m_impulse = 0f;
        } else {
            m_impulse *= 1f;//data.step.dtRatio;
            float P_x = m_impulse * m_u_x;
            float P_y = m_impulse * m_u_y;
            a.velocity.x -= a.invMass * P_x;
            a.velocity.y -= a.invMass * P_y;
            b.velocity.x += b.invMass * P_x;
            b.velocity.y += b.invMass * P_y;
        }
    }

    public void CalculateVelocityConstraint (float deltaTime) {
        if(m_u_mag == 0f) {
            return;
        }
        float diffvel_x = b.velocity.x - a.velocity.x;
        float diffvel_y = b.velocity.y - a.velocity.y;

        float Cdot = m_u_x * diffvel_x + m_u_y * diffvel_y;

        float impulse = -m_mass * (Cdot + m_bias + m_gamma * m_impulse);
        m_impulse += impulse;

        a.velocity.x -= a.invMass * impulse * m_u_x;
        a.velocity.y -= a.invMass * impulse * m_u_y;
        b.velocity.x += b.invMass * impulse * m_u_x;
        b.velocity.y += b.invMass * impulse * m_u_y;
    }

    public void SolvePositionConstraints (float deltaTime) {
	    if(!rope.enablePosCorrection) {
		    return;
	    }

        float diff_x = b.position.x - a.position.x;
        float diff_y = b.position.y - a.position.y;

        float length = Mathf.Sqrt(diff_x * diff_x + diff_y * diff_y);
        if(length == 0f) {
            return;
        }
        diff_x /= length;
        diff_y /= length;
        float C = length - rope.targetChainSegmentLength;
        C = Mathf.Clamp(C, -rope.jointMaxPosCorrection, rope.jointMaxPosCorrection);

        float impulse = -m_mass * C;

        a.TryMovePositionByDelta(-a.invMass * impulse * diff_x, -a.invMass * impulse * diff_y);
        b.TryMovePositionByDelta(b.invMass * impulse * diff_x, b.invMass * impulse * diff_y);
    }
    #endregion

    #region Unused experimental shit
    /*static float b2Cross (ref Vector2 a, ref Vector2 b) {
	    return a.x* b.y - a.y* b.x;
    }

    static Vector2 b2Cross (ref float s, ref Vector2 a) {
	    return new Vector2(-s* a.y, s* a.x);
    }

    public void CalculateForceType3 (float deltaTime) {
        Vector2 delta = b.position - a.position;
        float deltalength = delta.magnitude;
        if(deltalength == 0f) {
            return;
        }
        Vector2 dir = delta / deltalength;

        float relDist = (deltalength - rope.targetDistance);
        float relVelocity = Vector2.Dot(b.velocity - a.velocity, dir);

        float remove = relVelocity + relDist / deltaTime;
        Vector2 impulse = (remove / (a.invMass + b.invMass)) * dir;

        a.forces += impulse;
        b.forces -= impulse;
    }

    public void CalculateForceType2 (float deltaTime) {
        Vector2 delta = b.position - a.position;
        float deltalength = delta.magnitude;
        if(deltalength == 0f) {
            return;
        }
        Vector2 dir = delta / deltalength;

        float diff = (deltalength - rope.targetDistance) / (deltalength * (a.invMass + b.invMass));
        a.position += a.invMass * delta * diff;
        b.position -= b.invMass * delta * diff;
    }

    public void CalculateForce () {
        Vector3 delta = b.position - a.position;
        float mag = delta.magnitude;
        if(mag == 0f) {
            previousMag = 0f;
            return;
        }
        Vector3 normalized = delta / mag;

        float p = mag - rope.targetDistance;
        float d = mag - previousMag;
        i += mag - rope.targetDistance;

        force += (p * rope.pMultiplier + d * rope.dMultiplier + i * rope.iMultiplier) * normalized;
        previousMag = mag;
    }


    public void ApplyForce () {
        a.AddForce(force);
        b.AddForce(-force);
        force = Vector3.zero;
    }*/
    #endregion
}

public class MovingMass {
    public Vector2 position;
    public Vector2 forces;
    public float mass;
    public float invMass;
    public Vector2 velocity;
    public float colliderRadius;

    bool wasInsideTileLastCheck;
    public Vector2 prevPos;

    public bool hasCollidedUp;
    public bool hasCollidedDown;
    public bool hasCollidedLeft;
    public bool hasCollidedRight;

    public MovingMass (Vector2 position, float mass, float colliderRadius) {
        this.position = position;
        velocity = Vector2.zero;
        this.mass = mass;
        invMass = (mass == 0f || float.IsInfinity(mass)) ? 0 : 1f / mass;
        this.colliderRadius = colliderRadius;
    }

    public void AddForce (float x, float y) {
        forces.x += x;
        forces.y += y;
    }

    public void UpdatePosition (float deltaTime) {
        velocity.x += forces.x * invMass * deltaTime;
        velocity.y += forces.y * invMass * deltaTime;
        float deltaX = velocity.x * deltaTime;
        float deltaY = velocity.y * deltaTime;

        if(deltaX > 0 && !hasCollidedRight || deltaX < 0 && !hasCollidedLeft || true) {
            position.x += deltaX;
        }
        if(deltaY > 0 && !hasCollidedUp || deltaY < 0 && !hasCollidedDown || true) {
            position.y += deltaY;
        }
        forces.x = 0f;
        forces.y = 0f;
    }

    public void TryMovePositionByDelta (float deltaX, float deltaY) {
        if(deltaX > 0 && !hasCollidedRight || deltaX < 0 && !hasCollidedLeft || true) {
            position.x += deltaX;
        }
        if(deltaY > 0 && !hasCollidedUp || deltaY < 0 && !hasCollidedDown || true) {
            position.y += deltaY;
        }
    }

    public void UpdateCollision () {
        int currentTileX = Mathf.FloorToInt(position.x);
        int currentTileY = Mathf.FloorToInt(position.y);
        int prevTileX = Mathf.FloorToInt(prevPos.x);
        int prevTileY = Mathf.FloorToInt(prevPos.y);

        // Has moved to solid tile?
        bool newTileHasCollision = false;
        if(currentTileX != prevTileX || currentTileY != prevTileY) {
            if(TerrainManager.inst.GetGlobalIDAt(currentTileX, currentTileY, TerrainLayers.Ground, out int globalID)) {
                if(globalID != 0 && TerrainManager.inst.tiles.GetTileAssetFromGlobalID(globalID).hasCollision) {
                    newTileHasCollision = true;
                }
            }
        }

        if(newTileHasCollision || true) {
            float deltaX = position.x - prevPos.x;
            float deltaY = position.y - prevPos.y;
            Bounds2D bounds = new Bounds2D(new Vector2(prevPos.x - colliderRadius, prevPos.y - colliderRadius), new Vector2(prevPos.x + colliderRadius, prevPos.y + colliderRadius));
            //PhysicsPixel.DrawBounds(bounds, Color.red);
            Bounds2D queryBounds = bounds;
            queryBounds.ExtendByDelta(deltaX, deltaY);
            //PhysicsPixel.DrawBounds(queryBounds, Color.magenta);
            Debug.DrawLine(prevPos, prevPos + velocity * 0.1f, (hasCollidedLeft || hasCollidedRight) ? Color.red : Color.blue);

            int queryTileMinX = Mathf.FloorToInt(queryBounds.min.x);
            int queryTileMinY = Mathf.FloorToInt(queryBounds.min.y);
            int queryTileMaxX = Mathf.FloorToInt(queryBounds.max.x);
            int queryTileMaxY = Mathf.FloorToInt(queryBounds.max.y);

            float newDeltaX = deltaX;
            float newDeltaY = deltaY;

            for(int tileX = queryTileMinX; tileX <= queryTileMaxX; tileX++) {
                for(int tileY = queryTileMinY; tileY <= queryTileMaxY; tileY++) {
                    //PhysicsPixel.DrawBounds(new Bounds2D(new Vector2(tileX, tileY), new Vector2(tileX + 1f, tileY + 1f)), Color.green);
                    // If tile is solid...
                    if(TerrainManager.inst.GetGlobalIDAt(tileX, tileY, TerrainLayers.Ground, out int globalID)) {
                        if(globalID != 0 && TerrainManager.inst.tiles.GetTileAssetFromGlobalID(globalID).hasCollision) {
                            newDeltaY = PhysicsPixel.inst.MinimizeDeltaY(newDeltaY, tileX, tileY, ref bounds);
                            newDeltaX = PhysicsPixel.inst.MinimizeDeltaX(newDeltaX, tileX, tileY, ref bounds);
                        }
                    }
                }
            }

            hasCollidedUp = false;
            hasCollidedDown = false;
            hasCollidedRight = false;
            hasCollidedLeft = false;
            
            if(newDeltaY > deltaY) { //Down
                velocity.y = 0f;
                hasCollidedDown = true;
            } else if(newDeltaY < deltaY) { //Up
                velocity.y = 0f;
                hasCollidedUp = true;
            }
            if(newDeltaX > deltaX) { //Left
                velocity.x = 0f;
                hasCollidedLeft = true;
            } else if(newDeltaX < deltaX) { //Right
                velocity.x = 0f;
                hasCollidedRight = true;
            }

            position.x = prevPos.x + newDeltaX;
            position.y = prevPos.y + newDeltaY;
        }

        wasInsideTileLastCheck = newTileHasCollision;
        prevPos = new Vector2(position.x, position.y);
    }

    #region Awful Physics Code
    // I'm still not sure what this does even if I made it. It's magic.
    public bool RaycastTerrainBox (float origin_x, float origin_y, float dir_x, float dir_y, float maxDistance, out float dist, out bool hitWall) {

        // Voxel traversal preparation
        float pos0_x = origin_x;                float pos0_y = origin_y;
        float pos1_x = pos0_x;                  float pos1_y = pos0_y;
        float step_x = Mathf.Sign(dir_x);       float step_y = Mathf.Sign(dir_y);
        float tMax_x = IntBound(pos0_x, dir_x); float tMax_y = IntBound(pos0_y, dir_y);
        float tDelta_x = (dir_x != 0) ? (1f / dir_x * step_x) : 0f;
        float tDelta_y = (dir_y != 0) ? (1f / dir_y * step_y) : 0f;
        if(dir_x == 0 && dir_y == 0) {
            dist = 0f;
            hitWall = false;
            return false;
        }

        float invdir_x = dir_x == 0f ? 0f : 1f / dir_x;
        float invdir_y = dir_y == 0f ? 0f : 1f / dir_y;

        // Execute a 2D voxel traversal routine to find solid tile
        int tilePos_x, tilePos_y;
        int limtCounter = Mathf.CeilToInt(maxDistance) + 1;
        for(int i = limtCounter; i >= 0; i--) {
            tilePos_x = Mathf.FloorToInt(pos0_x);
            tilePos_y = Mathf.FloorToInt(pos0_y);
            //PhysicsPixel.DrawBounds(new Bounds2D(new Vector2(tilePos_x, tilePos_y), new Vector2(tilePos_x + 1f, tilePos_y + 1f)), new Color(0.8f, 0.4f, 0.07f));

            if(TerrainManager.inst.GetGlobalIDAt(tilePos_x, tilePos_y, TerrainLayers.Ground, out int globalID)) {
                if(globalID != 0 && TerrainManager.inst.tiles.GetTileAssetFromGlobalID(globalID).hasCollision) {
                    if(BoxRaycast(origin_x, origin_y, invdir_x, invdir_y, tilePos_x, tilePos_y, out float distance)) {
                        if(distance > maxDistance) {
                            dist = maxDistance;
                            hitWall = false;
                            return false;
                        } else {
                            dist = distance;
                            float hitPos_x = (origin_x + dir_x * distance);
                            float hitPos_y = (origin_y + dir_y * distance);
                            hitWall = Mathf.Approximately(hitPos_x, tilePos_x) || Mathf.Approximately(hitPos_x, tilePos_x + 1f);
                            return true;
                        }
                    }
                    dist = maxDistance;
                    hitWall = false;
                    return false;
                }
            }
            
            if(tMax_x < tMax_y) {
                pos0_x += step_x;
                tMax_x += tDelta_x;
            } else {
                pos0_y += step_y;
                tMax_y += tDelta_y;
            }
        }

        dist = maxDistance;
        hitWall = false;
        return false;
    }

    // I'm still not sure what this does even if I made it. It's magic
    // MAKE SURE THE DIRECTION IS INVERTED WITH (dir == 0? 0f : 1f/dir)
    public bool BoxRaycast (float origin_x, float origin_y, float dir_x, float dir_y, float box_x, float box_y, out float dist) {
        bool signDirX = dir_x < 0;
        bool signDirY = dir_y < 0;
        float bbox_x = 0f;
        float bbox_y = 0f;

        if(signDirX) {
            bbox_x = box_x + 1f;
            bbox_y = box_y + 1f;
        } else {
            bbox_x = box_x;
            bbox_y = box_y;
        }
        float tmin = (bbox_x - origin_x) * dir_x;
        if(signDirX) {
            bbox_x = box_x;
            bbox_y = box_y;
        } else {
            bbox_x = box_x + 1f;
            bbox_y = box_y + 1f;
        }
        float tmax = (bbox_x - origin_x) * dir_x;
        if(signDirY) {
            bbox_x = box_x + 1f;
            bbox_y = box_y + 1f;
        } else {
            bbox_x = box_x;
            bbox_y = box_y;
        }
        float tymin = (bbox_y - origin_y) * dir_y;
        if(signDirY) {
            bbox_x = box_x;
            bbox_y = box_y;
        } else {
            bbox_x = box_x + 1f;
            bbox_y = box_y + 1f;
        }
        float tymax = (bbox_y - origin_y) * dir_y;

        if((tmin > tymax) || (tymin > tmax)) {
            dist = 0f;
            return false;
        }
        if(tymin > tmin) {
            tmin = tymin;
        }
        if(tymax < tmax) {
            tmax = tymax;
        }

        dist = tmin;
        return true;
    }

    static float IntBound (float s, float ds) {
        // Find the smallest positive t such that s+t*ds is an integer.
        if(ds < 0) {
            return IntBound(-s, -ds);
        } else {
            s = Modulo(s, 1);
            // problem is now s+t*ds = 1
            return (1 - s) / ds;
        }
    }

    static float Modulo (float value, float modulus) {
        return (value % modulus + modulus) % modulus;
    }

    static int Modulo (int value, int modulus) {
        return (value % modulus + modulus) % modulus;
    }
    #endregion Awful Physics Code
}
