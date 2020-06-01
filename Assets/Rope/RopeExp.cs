using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeExp : MonoBehaviour { 

    [Header("Monobehaviour References")]
    public Camera cam;
    public LineRenderer lineRenderer;
    public Transform visualAnchor;
    public RigidbodyPixel physicAnchor;
    public Transform leftAnchorPoint;
    public Transform rightAnchorPoint;
    public Transform holdPoint;
    public PlayerController owner;

    [Header("Rope Parameters")]
    public int segmentCount;
    public float targetChainSegmentLength = 0.333f;
    public float targetAnchorSegmentLength = 0.333f;
    public float segmentMass = 0.5f;
    public float anchorMass = 16f;
    public float collRadius = 0.25f;
    public float smoothTurn = 0.1f;
    public float turnSpeed = 1f;

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
    public bool posCorrectionUseMass = false;
    public float posCorrectionFactor = 1f;

    RigidbodyPixel attachedBodyLeft;
    LivingEntity attachedEntityLeft;
    RigidbodyPixel attachedBodyRight;
    LivingEntity attachedEntityRight;
    Vector2 attachedBodyDeltaLeft;
    Vector2 attachedBodyDeltaRight;
    Vector2 pinPoint = Vector2.zero;

    List<RopeJoint> distanceJoints;
    List<RopeMovingMass> movingMasses;
    Vector3[] interpositions;
    Vector3[] positions;
    Vector3[] lastPositions;

    void Start () {
        GenerateRope();

        physicAnchor.OnWeakCollision += AnchorHitSomething; //Should auto-unsubscribe
    }

    void AnchorHitSomething (RigidbodyPixel target) {
        if(target.name == "Player") {
            return;
        }
        
        float leftDist = Vector2.Distance(target.transform.position, leftAnchorPoint.position);
        float rightDist = Vector2.Distance(target.transform.position, rightAnchorPoint.position);

        if(leftDist < rightDist) {
            if(attachedBodyLeft == null && target != attachedBodyRight) {
                if(target.GetBoundFromCollider().Overlaps(leftAnchorPoint.position)) {
                    attachedBodyLeft = target;
                    attachedBodyDeltaLeft = attachedBodyLeft.transform.position - leftAnchorPoint.position;

                    attachedEntityLeft = attachedBodyLeft.GetComponent<LivingEntity>();
                }
            }
        } else {
            if(attachedBodyRight == null && target != attachedBodyLeft) {
                if(target.GetBoundFromCollider().Overlaps(rightAnchorPoint.position)) {
                    attachedBodyRight = target;
                    attachedBodyDeltaRight = attachedBodyRight.transform.position - rightAnchorPoint.position;

                    attachedEntityRight = attachedBodyRight.GetComponent<LivingEntity>();
                }
            }
        }
        
    }

    private void Update () {

        // THROW DEBUG
        if(Input.GetKey(KeyCode.C)) {
            /*for(int i = 0; i < movingMasses.Count; i++) {
                movingMasses[i].position = pinPoint;
                movingMasses[i].prevPos = pinPoint;
                movingMasses[i].SetVelocity(0f, 0f);
            }*/
            
            physicAnchor.ignoreProjectileOwnerUntilHitWall = GameManager.inst.allPlayers[0].rbody;
        }
        if(Input.GetKeyUp(KeyCode.C)) {
            pinPoint = cam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            Vector2 aimPoint = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (aimPoint - pinPoint).normalized;
            movingMasses[movingMasses.Count - 1].SetVelocity(dir.x * 50f, dir.y * 50f);
        }
        // THROW DEBUG

        // CONTROL DEBUG
        if(Input.GetKey(KeyCode.V)) {
            pinPoint = cam.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            Vector2 aimPoint = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (aimPoint - pinPoint).normalized;

            float targetLength = segmentCount * targetChainSegmentLength;
            Vector2 targetPosition = aimPoint + dir * targetLength;
            Vector2 targetDirection = (targetPosition - (Vector2)physicAnchor.transform.position).normalized;
            Debug.DrawLine(pinPoint, pinPoint + targetDirection);
            Debug.DrawLine(pinPoint, targetPosition);

            movingMasses[segmentCount - 1].AddVelocity(targetDirection.x * Time.deltaTime * 30f, targetDirection.y * Time.deltaTime * 30f);
        }
        // CONTROL DEBUG

        float velDiff = physicAnchor.lastVelocity.magnitude - physicAnchor.velocity.magnitude;
        if(velDiff > 4f) {
            float healthDamage = velDiff * 0.2f;
            if(attachedEntityLeft != null && attachedEntityLeft.HitEntity(healthDamage)) {
                attachedEntityLeft = null;
                attachedBodyLeft = null;
            }
            if(attachedEntityRight != null && attachedEntityRight.HitEntity(healthDamage)) {
                attachedEntityRight = null;
                attachedBodyRight = null;
            }
        }

        if(Input.GetKey(KeyCode.X)) {
            Vector2 diff = GameManager.inst.allPlayers[0].GetHeadPosition() - (Vector2)physicAnchor.transform.position;
            float dist = diff.magnitude;
            Vector2 dir = diff / dist;

            if(dist > 3f) {
                physicAnchor.velocity += new Vector2(dir.x * .2f, dir.y * .2f);
            }
        }

        DrawRope();
    }

    private void FixedUpdate () {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        if(Input.GetMouseButton(0)) {
            Vector2 aimPoint = cam.ScreenToWorldPoint(Input.mousePosition);

            var damp = 12f;
            var maxSpeed = 30f;
            var target = aimPoint;
            physicAnchor.velocity = Vector2.ClampMagnitude(physicAnchor.velocity, maxSpeed);

            var n1 = physicAnchor.velocity - ((Vector2)physicAnchor.transform.position - target) * damp * damp * Time.deltaTime;
            var n2 = 1 + damp * Time.deltaTime;
            physicAnchor.velocity += (n1 / (n2 * n2)) - (physicAnchor.velocity);

        }

        ConstraintRope();
        SimulateRope();

        GetLastPositions();
        GetPositions();

        sw.Stop();
        long ticks = sw.ElapsedTicks;
        double milliseconds = (ticks / (double)System.Diagnostics.Stopwatch.Frequency) * 1000;
        //Debug.Log(milliseconds);
        
        if(attachedBodyLeft != null) {
            attachedBodyLeft.velocity = Vector2.zero;

            Vector2 targetPosition = leftAnchorPoint.position + (Vector3)attachedBodyDeltaLeft;
            attachedBodyLeft.MovePosition(targetPosition);
            if((targetPosition - (Vector2)attachedBodyLeft.transform.position).sqrMagnitude > 4f) {
                attachedBodyLeft = null;
            }
        }
        if(attachedBodyRight != null) {
            attachedBodyRight.velocity = Vector2.zero;

            Vector2 targetPosition = rightAnchorPoint.position + (Vector3)attachedBodyDeltaRight;
            attachedBodyRight.MovePosition(targetPosition);
            if((targetPosition - (Vector2)attachedBodyRight.transform.position).sqrMagnitude > 4f) {
                attachedBodyRight = null;
            }
        }
    }




    private void GenerateRope () {
        distanceJoints = new List<RopeJoint>();
        movingMasses = new List<RopeMovingMass>();
        positions = new Vector3[segmentCount];
        lastPositions = new Vector3[segmentCount];
        interpositions = new Vector3[segmentCount];

        Vector2 initPos = transform.position;
        
        for(int i = 0; i < segmentCount; i++) {
            RopeMovingMass mm;
            if(i >= segmentCount - 1) {
                mm = new RopeMovingMass(initPos, anchorMass, collRadius);
            } else {
                mm = new RopeMovingMass(initPos, segmentMass, collRadius);
            }
            movingMasses.Add(mm);

            initPos += Vector2.down * targetChainSegmentLength;
        }

        for(int i = 1; i < segmentCount; i++) {
            if(i == segmentCount - 1) {
                distanceJoints.Add(new RopeJoint(this, movingMasses[i - 1], movingMasses[i], targetAnchorSegmentLength));
            } else {
                distanceJoints.Add(new RopeJoint(this, movingMasses[i - 1], movingMasses[i], targetChainSegmentLength));
            }
        }

        movingMasses[movingMasses.Count - 1].Pair(physicAnchor);
    }

    /// <summary>
    /// Simulates the rope (joints and moving masses) for one frame (Must me run fixed frame interval)
    /// </summary>
    private void SimulateRope () {
        float deltaTimeStep = Time.deltaTime / simulationSteps;
        float deltaTime = Time.deltaTime;

        // Multiple simulation step are needed to get a stable result, but it's utterly slow.
        for(int simstep = 0; simstep < simulationSteps; simstep++) {
            for(int i = 0; i < movingMasses.Count; i++) {
                movingMasses[i].UpdatePosition(deltaTimeStep);
            }
            for(int j = distanceJoints.Count - 1; j >= 0; j--) {
                distanceJoints[j].InitiateVelocityConstraint(deltaTimeStep);
                distanceJoints[j].CalculateVelocityConstraint(deltaTimeStep);
                distanceJoints[j].SolvePositionConstraints(deltaTimeStep);
            }
            movingMasses[0].position = pinPoint;
            movingMasses[0].SetVelocity(0f, 0f);
            movingMasses[0].forces = Vector2.zero;
        }

        // Now we apply the gravity and move the point accordingly
        for(int i = 0; i < movingMasses.Count; i++) {
            RopeMovingMass mm = movingMasses[i];

            if(i == movingMasses.Count - 1) {
                mm.MulVelocity(1.0f / (1.0f + deltaTime * anchorAirDrag));
            } else {
                mm.MulVelocity(1.0f / (1.0f + deltaTime * chainAirDrag));
            }

            // Not sure why this is needed but it is.
            if(i == movingMasses.Count - 1) {
                mm.UpdateCollision();
            }
            mm.AddForce(gravity.x * mm.mass, gravity.y * mm.mass);
            mm.UpdatePosition(deltaTime);
        }

        // This prevents the anchor from being upside down when sitting on the ground by preventing a point from dipping lower than the anchor.
        if(physicAnchor.isCollidingWallDown) {
            movingMasses[movingMasses.Count - 2].position = new Vector2(
                movingMasses[movingMasses.Count - 2].position.x,
                Mathf.Max(movingMasses[movingMasses.Count - 2].position.y, physicAnchor.transform.position.y)
            );
        }

        // In case of collision, play a particle effect and hurt the entity. Note that the reference of pooled entity hasn't been taked into
        // account correctly all throughout the game's code. This is just a simple patch to a bigger issue.
        if(physicAnchor.lastVelocity.magnitude > 10f) {
            if(physicAnchor.velocity.magnitude > 5f) {
                goto exit;
            }
            ParticleManager.inst.PlayFixedParticle(physicAnchor.GetPosition() + Vector3.down, 4);
            bool hadCollision = physicAnchor.hadCollisionDown || physicAnchor.hadCollisionUp || physicAnchor.hadCollisionLeft || physicAnchor.hadCollisionRight;
            if(!hadCollision) {
                goto exit;
            }
            bool hasCollision = physicAnchor.isCollidingDown || physicAnchor.isCollidingUp || physicAnchor.isCollidingLeft || physicAnchor.isCollidingRight;
            if(hasCollision) {
                ParticleManager.inst.PlayFixedParticle(physicAnchor.GetPosition() + Vector3.down, 4);
            }
        }
        exit:;
    }

    /// <summary>
    /// Constrain one end of the rope to the hold point
    /// </summary>
    private void ConstraintRope () {
        pinPoint = holdPoint.position;

        // DEBUG BRING BACK
        if(Input.GetKey(KeyCode.Z)) {
            for(int i = 0; i < movingMasses.Count; i++) {
                movingMasses[i].position = pinPoint;
                movingMasses[i].prevPos = pinPoint;
                movingMasses[i].SetVelocity(0f, 0f);
            }
            physicAnchor.transform.position = movingMasses[movingMasses.Count - 1].position;
            physicAnchor.ignoreProjectileOwnerUntilHitWall = owner.rbody;
        }
    }

    /// <summary>
    /// Animates the anchor and sets the correct positions to the line renderer.
    /// </summary>
    private void DrawRope () {
        float interfactor = InterpolationManager.InterpolationFactor;
        for(int i = 0; i < segmentCount; i++) {
            interpositions[i].Set(Mathf.Lerp(lastPositions[i].x, positions[i].x, interfactor), Mathf.Lerp(lastPositions[i].y, positions[i].y, interfactor), 0f);
        }

        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(interpositions);

        AnimateAnchor();
    }

    /// <summary>
    /// Animates and spawn the correct particles around the anchor.
    /// </summary>
    private void AnimateAnchor () {
        float t = 1f - Mathf.Pow(1f - smoothTurn, Time.deltaTime * 30f);
        float angle = visualAnchor.eulerAngles.z + 90f;

        Vector2 targetDir = (interpositions[segmentCount - 2] - interpositions[segmentCount - 1]).normalized;
        Vector2 currDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector2 newDir = Vector3.Slerp(targetDir, currDir, t * turnSpeed);

        visualAnchor.position = interpositions[segmentCount - 2];
        visualAnchor.eulerAngles = Vector3.forward * Mathf.Atan2(-newDir.x, newDir.y) * Mathf.Rad2Deg;

        // Scraping the ground particle effect.
        float lastVelocityMag = new Vector2(
            movingMasses[segmentCount - 1].GetVelocityX(),
            movingMasses[segmentCount - 1].GetVelocityY()
        ).magnitude;
        if(lastVelocityMag > 2f) {
            if(PhysicsPixel.inst.IsPointSolid(leftAnchorPoint.position)) {
                if(Random.Range(0, 8) == 0) {
                    ParticleManager.inst.PlayFixedParticle(leftAnchorPoint.position, 7);
                }
                ParticleManager.inst.PlayFixedParticle(leftAnchorPoint.position, 6);
            }
            if(PhysicsPixel.inst.IsPointSolid(rightAnchorPoint.position)) {
                if(Random.Range(0, 8) == 0) {
                    ParticleManager.inst.PlayFixedParticle(rightAnchorPoint.position, 7);
                }
                ParticleManager.inst.PlayFixedParticle(rightAnchorPoint.position, 6);
            }
        }
    }

    /// <summary>
    /// Fills the array of old positions with the current array of positions.
    /// </summary>
    private void GetLastPositions () {
        for(int i = 0; i < segmentCount; i++) {
            lastPositions[i] = positions[i];
        }
    }

    /// <summary>
    /// Fills the array of positions from the moving masses.
    /// </summary>
    private void GetPositions () {
        for(int i = 0; i < movingMasses.Count; i++) {
            positions[i] = movingMasses[i].position;
        }
    }
}


/// <summary>
/// A constraint applied to two RopeMovingMass, similar to box2d's distance joint, used to simulate rope segments.
/// </summary>
public class RopeJoint {
    public RopeExp rope;

    public RopeMovingMass a;
    public RopeMovingMass b;

    // Failed attempt at reading Box2D's variables
    const float b2_linearSlop = 0.005f;
    float m_u_x;
    float m_u_y;
    float m_u_mag;
    float m_impulse;
    float m_mass;
    float m_gamma;
    float m_bias;

    float spring_length = 0.3333f;

    public RopeJoint (RopeExp rope, RopeMovingMass a, RopeMovingMass b, float length) {
        this.rope = rope;
        this.a = a;
        this.b = b;
        spring_length = length;
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
            float distError = length - spring_length;
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
            a.AddVelocity(-a.invMass * P_x, -a.invMass * P_y);
            b.AddVelocity(b.invMass * P_x, b.invMass * P_y);
        }
    }

    public void CalculateVelocityConstraint (float deltaTime) {
        if(m_u_mag == 0f) {
            return;
        }
        float diffvel_x = b.GetVelocityX() - a.GetVelocityX();
        float diffvel_y = b.GetVelocityY() - a.GetVelocityY();

        float Cdot = m_u_x * diffvel_x + m_u_y * diffvel_y;

        float impulse = -m_mass * (Cdot + m_bias + m_gamma * m_impulse);
        m_impulse += impulse;

        a.AddVelocity(-a.invMass * impulse * m_u_x, -a.invMass * impulse * m_u_y);
        b.AddVelocity(b.invMass * impulse * m_u_x, b.invMass * impulse * m_u_y);
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
        float C = length - spring_length;
        C = Mathf.Clamp(C, -rope.jointMaxPosCorrection, rope.jointMaxPosCorrection);
        
        float impulse = -(rope.posCorrectionUseMass ? m_mass : 1f) * C;

        float aInvMass = rope.posCorrectionUseMass ? a.invMass : 1f;
        float bInvMass = rope.posCorrectionUseMass ? b.invMass : 1f;
        a.TryMovePositionByDelta(-aInvMass * impulse * diff_x, -aInvMass * impulse * diff_y);
        b.TryMovePositionByDelta( bInvMass * impulse * diff_x,  bInvMass * impulse * diff_y);
    }
    #endregion
    
}

/// <summary>
/// Physics object using euler's model used to simulate ropes. I failed at implementing collision to the ropes.
/// </summary>
public class RopeMovingMass {
    Vector2 _position;
    public Vector2 forces;
    public float mass;
    public float invMass;
    public float colliderRadius;
    Vector2 velocity;

    public Vector2 position {
        get {
            return _position;
        }
        set {
            _position = value;
            if(isPaired) {
                pairing.transform.position = _position;
            }
        }
    }

    bool wasInsideTileLastCheck;
    public Vector2 prevPos;
    
    bool isPaired;
    RigidbodyPixel pairing;

    public RopeMovingMass (Vector2 position, float mass, float colliderRadius) {
        _position = position;
        velocity = Vector2.zero;
        this.mass = mass;
        invMass = (mass == 0f || float.IsInfinity(mass)) ? 0 : 1f / mass;
        this.colliderRadius = colliderRadius;
    }

    public void Pair (RigidbodyPixel pairing) {
        this.pairing = pairing;
        isPaired = true;
    }

    public void AddForce (float x, float y) {
        forces.x += x;
        forces.y += y;
    }

    public void AddVelocity (float x, float y) {
        if(isPaired) {
            pairing.velocity.x += x;
            pairing.velocity.y += y;
        } else {
            velocity.x += x;
            velocity.y += y;
        }
    }

    public void MulVelocity (float x) {
        if(isPaired) {
            pairing.velocity.x *= x;
            pairing.velocity.y *= x;
        } else {
            velocity.x *= x;
            velocity.y *= x;
        }
    }

    public void SetVelocity (float x, float y) {
        if(isPaired) {
            pairing.velocity.x = x;
            pairing.velocity.y = y;
        } else {
            velocity.x = x;
            velocity.y = y;
        }
    }

    public float GetVelocityX () {
        if(isPaired) {
            return pairing.velocity.x;
        } else {
            return velocity.x;
        }
    }

    public float GetVelocityY () {
        if(isPaired) {
            return pairing.velocity.y;
        } else {
            return velocity.y;
        }
    }

    public void UpdatePosition (float deltaTime) {
        float deltaX, deltaY;
        if(isPaired) {
            pairing.velocity.x += forces.x * invMass * deltaTime;
            pairing.velocity.y += forces.y * invMass * deltaTime;
            
            deltaX = pairing.velocity.x * deltaTime;
            deltaY = pairing.velocity.y * deltaTime;

            _position.x += deltaX;
            _position.y += deltaY;

            forces.x = 0f;
            forces.y = 0f;

            return;
        }

        velocity.x += forces.x * invMass * deltaTime;
        velocity.y += forces.y * invMass * deltaTime;

        deltaX = velocity.x * deltaTime;
        deltaY = velocity.y * deltaTime;

        _position.x += deltaX;
        _position.y += deltaY;

        forces.x = 0f;
        forces.y = 0f;
    }

    public void TryMovePositionByDelta (float deltaX, float deltaY) {
        if(isPaired) {
            _position.x += deltaX;
            _position.y += deltaY;
            pairing.deltaDischarge += new Vector2(deltaX, deltaY);
            return;
        }

        _position.x += deltaX;
        _position.y += deltaY;
    }

    public void UpdateCollision () {
        if(isPaired) {
            _position = pairing.transform.position;
            return;
        }

        float deltaX = _position.x - prevPos.x;
        float deltaY = _position.y - prevPos.y;
        Bounds2D bounds = new Bounds2D(new Vector2(prevPos.x - colliderRadius, prevPos.y - colliderRadius), new Vector2(prevPos.x + colliderRadius, prevPos.y + colliderRadius));
        Bounds2D queryBounds = bounds;

        queryBounds.ExtendByDelta(deltaX, deltaY);

        float newDeltaX = deltaX;
        float newDeltaY = deltaY;

        if(deltaX > deltaY) {
            MoveDeltaY(ref queryBounds, ref bounds, deltaY, ref newDeltaY);
            MoveDeltaX(ref queryBounds, ref bounds, deltaX, ref newDeltaX);
        } else {
            MoveDeltaX(ref queryBounds, ref bounds, deltaX, ref newDeltaX);
            MoveDeltaY(ref queryBounds, ref bounds, deltaY, ref newDeltaY);
        }

        _position.x = prevPos.x + newDeltaX;
        _position.y = prevPos.y + newDeltaY;
        prevPos = new Vector2(_position.x, _position.y);
    }

    #region Unused Physics Code
    void MoveDeltaY (ref Bounds2D queryBounds, ref Bounds2D bounds, float deltaY, ref float newDeltaY) {
        queryBounds = bounds;
        queryBounds.ExtendByDelta(0f, deltaY);
        int queryTileMinX = Mathf.FloorToInt(queryBounds.min.x);
        int queryTileMinY = Mathf.FloorToInt(queryBounds.min.y);
        int queryTileMaxX = Mathf.CeilToInt(queryBounds.max.x);
        int queryTileMaxY = Mathf.CeilToInt(queryBounds.max.y);

        for(int tileX = queryTileMinX; tileX <= queryTileMaxX; tileX++) {
            for(int tileY = queryTileMinY; tileY <= queryTileMaxY; tileY++) {
                //PhysicsPixel.DrawBounds(new Bounds2D(new Vector2(tileX, tileY), new Vector2(tileX + 1f, tileY + 1f)), Color.green);
                // If tile is solid...
                if(TerrainManager.inst.GetGlobalIDAt(tileX, tileY, TerrainLayers.Ground, out int globalID)) {
                    if(globalID != 0 && TerrainManager.inst.tiles.GetTileAssetFromGlobalID(globalID).hasCollision) {
                        newDeltaY = PhysicsPixel.inst.MinimizeDeltaY(newDeltaY, tileX, tileY, ref bounds);
                    }
                }
            }
        }
    }

    void MoveDeltaX (ref Bounds2D queryBounds, ref Bounds2D bounds, float deltaX, ref float newDeltaX) {
        queryBounds = bounds;
        queryBounds.ExtendByDelta(deltaX, 0f);
        int queryTileMinX = Mathf.FloorToInt(queryBounds.min.x);
        int queryTileMinY = Mathf.FloorToInt(queryBounds.min.y);
        int queryTileMaxX = Mathf.CeilToInt(queryBounds.max.x);
        int queryTileMaxY = Mathf.CeilToInt(queryBounds.max.y);

        for(int tileX = queryTileMinX; tileX <= queryTileMaxX; tileX++) {
            for(int tileY = queryTileMinY; tileY <= queryTileMaxY; tileY++) {
                //PhysicsPixel.DrawBounds(new Bounds2D(new Vector2(tileX, tileY), new Vector2(tileX + 1f, tileY + 1f)), Color.green);
                // If tile is solid...
                if(TerrainManager.inst.GetGlobalIDAt(tileX, tileY, TerrainLayers.Ground, out int globalID)) {
                    if(globalID != 0 && TerrainManager.inst.tiles.GetTileAssetFromGlobalID(globalID).hasCollision) {
                        newDeltaX = PhysicsPixel.inst.MinimizeDeltaX(newDeltaX, tileX, tileY, ref bounds);
                    }
                }
            }
        }
    }
    #endregion
}
