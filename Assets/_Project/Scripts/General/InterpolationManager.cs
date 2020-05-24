using UnityEngine;
using System.Collections;

public class InterpolationManager : MonoBehaviour {
    private float[] m_lastFixedUpdateTimes;
    private int m_newTimeIndex;
    public static float InterpolationFactor {
        get;
        private set;
    }

    public void Awake () {
        m_lastFixedUpdateTimes = new float[2];
        m_newTimeIndex = 0;
    }

    public void FixedUpdate () {
        m_newTimeIndex = OldTimeIndex();
        m_lastFixedUpdateTimes[m_newTimeIndex] = Time.fixedTime;
    }

    public void Update () {
        float newerTime = m_lastFixedUpdateTimes[m_newTimeIndex];
        float olderTime = m_lastFixedUpdateTimes[OldTimeIndex()];

        if(newerTime != olderTime) {
            InterpolationFactor = (Time.time - newerTime) / (newerTime - olderTime);
        } else {
            InterpolationFactor = 1;
        }
    }

    private int OldTimeIndex () {
        return (m_newTimeIndex == 0 ? 1 : 0);
    }
}