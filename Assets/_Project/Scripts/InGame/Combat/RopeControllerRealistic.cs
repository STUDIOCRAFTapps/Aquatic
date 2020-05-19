using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeControllerRealistic : MonoBehaviour {
    //Objects that will interact with the rope
    public Transform sourceObject;
    public Transform hangObject;

    //Line renderer used to display the rope
    LineRenderer lineRenderer;

    //A list with all rope section
    public List<RopeSection> ropeSections = new List<RopeSection>();

    //Rope data
    [Range(0.25f,2f)]
    public float ropeSegmentLength = 1f;
    public int segmentCount = 16;

    //Data we can change to change the properties of the rope
    //Spring constant
    public float spring = 40f;
    //Damping from rope friction constant
    public float damp = 2f;
    //Damping from air resistance constant
    [Range(0f, 0.999f)]
    public float airRes = 0.05f;
    //Mass of one rope section
    public float sectionMass = 0.2f;
    public float hangMass = 0.01f;
    public Vector2 gravity = Vector2.down * 10f;

    Vector3[] displayPositions;

    void Start () {
        lineRenderer = GetComponent<LineRenderer>();
        
        Vector3 pos = sourceObject.position;
        for(int i = segmentCount - 1; i >= 0; i--) {
            ropeSections.Add(new RopeSection(pos));
            pos.y -= ropeSegmentLength;
        }

        displayPositions = new Vector3[ropeSections.Count];
    }

    private void Update () {
        hangObject.position = ropeSections[0].pos;
        Vector2 first = ropeSections[0].pos;
        Vector2 second = ropeSections[1].pos;
        hangObject.eulerAngles = Vector3.forward * (Mathf.Atan2(second.x - first.x, first.y - second.y) * Mathf.Rad2Deg + 180f);
    }

    void FixedUpdate () {
        if(ropeSections.Count > 0) {
            int iterations = 1;
            
            float timeStep = Time.deltaTime / iterations;

            for(int i = 0; i < iterations; i++) {
                UpdateRopeSimulation(ropeSections, timeStep);
            }
        }
        
        DisplayRope();
    }

    //Display the rope with a line renderer
    private void DisplayRope () {
        for(int i = 0; i < ropeSections.Count; i++) {
            displayPositions[i] = ropeSections[i].pos;
        }

        lineRenderer.positionCount = displayPositions.Length;
        lineRenderer.SetPositions(displayPositions);
    }

    private void UpdateRopeSimulation (List<RopeSection> allRopeSections, float timeStep) {
        //Move the last position, which is the top position, to what the rope is attached to
        RopeSection lastRopeSection = allRopeSections[allRopeSections.Count - 1];
        lastRopeSection.pos = sourceObject.position;
        allRopeSections[allRopeSections.Count - 1] = lastRopeSection;

        //
        //Calculate the next pos and vel with Forward Euler
        //
        //Calculate acceleration in each rope section which is what is needed to get the next pos and vel
        List<Vector3> accelerations = CalculateAccelerations(allRopeSections);
        List<RopeSection> nextPosVelForwardEuler = new List<RopeSection>();

        //Loop through all line segments (except the last because it's always connected to something)
        for(int i = 0; i < allRopeSections.Count - 1; i++) {
            RopeSection thisRopeSection = RopeSection.zero;

            //Forward Euler
            //vel = vel + acc * t
            thisRopeSection.vel = allRopeSections[i].vel + accelerations[i] * timeStep;

            //pos = pos + vel * t
            thisRopeSection.pos = allRopeSections[i].pos + allRopeSections[i].vel * timeStep;

            //Save the new data in a temporarily list
            nextPosVelForwardEuler.Add(thisRopeSection);
        }

        //Add the last which is always the same because it's attached to something
        nextPosVelForwardEuler.Add(allRopeSections[allRopeSections.Count - 1]);


        //
        //Calculate the next pos with Heun's method (Improved Euler)
        //
        //Calculate acceleration in each rope section which is what is needed to get the next pos and vel
        List<Vector3> accelerationFromEuler = CalculateAccelerations(nextPosVelForwardEuler);
        List<RopeSection> nextPosVelHeunsMethod = new List<RopeSection>();

        //Loop through all line segments (except the last because it's always connected to something)
        for(int i = 0; i < allRopeSections.Count - 1; i++) {
            RopeSection thisRopeSection = RopeSection.zero;

            //Heuns method
            //vel = vel + (acc + accFromForwardEuler) * 0.5 * t
            thisRopeSection.vel = allRopeSections[i].vel + (accelerations[i] + accelerationFromEuler[i]) * 0.5f * timeStep;

            //pos = pos + (vel + velFromForwardEuler) * 0.5f * t
            thisRopeSection.pos = allRopeSections[i].pos + (allRopeSections[i].vel + nextPosVelForwardEuler[i].vel) * 0.5f * timeStep;

            //Save the new data in a temporarily list
            nextPosVelHeunsMethod.Add(thisRopeSection);
        }

        //Add the last which is always the same because it's attached to something
        nextPosVelHeunsMethod.Add(allRopeSections[allRopeSections.Count - 1]);



        //From the temp list to the main list
        for(int i = 0; i < allRopeSections.Count; i++) {
            allRopeSections[i] = nextPosVelForwardEuler[i];
        }


        //Implement maximum stretch to avoid numerical instabilities
        //May need to run the algorithm several times
        int maximumStretchIterations = 1;

        for(int i = 0; i < maximumStretchIterations; i++) {
            ImplementMaximumStretch(allRopeSections);
        }
    }

    #region Calculate Acceleration
    private List<Vector3> CalculateAccelerations (List<RopeSection> allRopeSections) {
        List<Vector3> accelerations = new List<Vector3>();

        //Spring constant
        float k = spring;
        //Damping constant
        float d = damp;
        //Damping constant from air resistance
        float a = airRes;
        //Mass of one rope section
        float m = sectionMass;
        //How long should the rope section be
        float wantedLength = ropeSegmentLength;


        //Calculate all forces once because some sections are using the same force but negative
        List<Vector3> allForces = new List<Vector3>();

        for(int i = 0; i < allRopeSections.Count - 1; i++) {
            //From Physics for game developers book
            //The force exerted on body 1
            //pos1 (above) - pos2
            Vector3 vectorBetween = allRopeSections[i + 1].pos - allRopeSections[i].pos;
            float distanceBetween = vectorBetween.magnitude;
            Vector3 dir = vectorBetween.normalized;
            float springForce = k * (distanceBetween - wantedLength);


            //Damping from rope friction 
            //vel1 (above) - vel2
            float frictionForce = d * ((Vector3.Dot(allRopeSections[i + 1].vel - allRopeSections[i].vel, vectorBetween)) / distanceBetween);


            //The total force on the spring
            Vector3 springForceVec = -(springForce + frictionForce) * dir;

            //This is body 2 if we follow the book because we are looping from below, so negative
            springForceVec = -springForceVec;
            allForces.Add(springForceVec);
        }


        //Loop through all line segments (except the last because it's always connected to something)
        //and calculate the acceleration
        for(int i = 0; i < allRopeSections.Count - 1; i++) {
            Vector3 springForce = Vector3.zero;

            //Spring 1 - above
            springForce += allForces[i];

            //Spring 2 - below
            //The first spring is at the bottom so it doesnt have a section below it
            if(i != 0) {
                springForce -= allForces[i - 1];
            }

            //Damping from air resistance, which depends on the square of the velocity
            float vel = allRopeSections[i].vel.magnitude;
            Vector3 dampingForce = a * vel * vel * allRopeSections[i].vel.normalized;

            //The mass attached to this spring
            float springMass = m;

            //end of the rope is attached to a box with a mass
            if(i == 0) {
                springMass += hangMass;
            }

            //Force from gravity
            Vector3 grav = springMass * (Vector3)gravity;

            //The total force on this spring
            Vector3 totalForce = springForce + grav - dampingForce;

            //Calculate the acceleration a = F / m
            Vector3 acceleration = totalForce / springMass;

            accelerations.Add(acceleration);
        }

        //The last line segment's acc is always 0 because it's attached to something
        accelerations.Add(Vector3.zero);


        return accelerations;
    }
    #endregion

    #region Stretch & Compression
    //Implement maximum stretch to avoid numerical instabilities
    private void ImplementMaximumStretch (List<RopeSection> allRopeSections) {
        //Make sure each spring are not less compressed than 90% nor more stretched than 110%
        float maxStretch = 1.1f;
        float minStretch = 0.9f;

        //Loop from the end because it's better to adjust the top section of the rope before the bottom
        //And the top of the rope is at the end of the list
        for(int i = allRopeSections.Count - 1; i > 0; i--) {
            RopeSection topSection = allRopeSections[i];

            RopeSection bottomSection = allRopeSections[i - 1];

            //The distance between the sections
            float dist = (topSection.pos - bottomSection.pos).magnitude;

            //What's the stretch/compression
            float stretch = dist / ropeSegmentLength;

            if(stretch > maxStretch) {
                //How far do we need to compress the spring?
                float compressLength = dist - (ropeSegmentLength * maxStretch);

                //In what direction should we compress the spring?
                Vector3 compressDir = (topSection.pos - bottomSection.pos).normalized;
                Vector3 change = compressDir * compressLength;

                MoveSection(change, i - 1);
            } else if(stretch < minStretch) {
                //How far do we need to stretch the spring?
                float stretchLength = (ropeSegmentLength * minStretch) - dist;

                //In what direction should we compress the spring?
                Vector3 stretchDir = (bottomSection.pos - topSection.pos).normalized;
                Vector3 change = stretchDir * stretchLength;
                MoveSection(change, i - 1);
            }
        }
    }

    //Move a rope section based on stretch/compression
    private void MoveSection (Vector3 finalChange, int listPos) {
        RopeSection bottomSection = ropeSections[listPos];

        //Move the bottom section
        Vector3 pos = bottomSection.pos;
        pos += finalChange;
        bottomSection.pos = pos;
        ropeSections[listPos] = bottomSection;
    }
    #endregion
}

//A class that will hold information about each rope section
public struct RopeSection {
    public Vector3 pos;
    public Vector3 vel;

    //To write RopeSection.zero
    public static readonly RopeSection zero = new RopeSection(Vector3.zero);

    public RopeSection (Vector3 pos) {
        this.pos = pos;

        this.vel = Vector3.zero;
    }
}