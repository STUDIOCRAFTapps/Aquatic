using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class PerformanceTest : MonoBehaviour {
    public int iterations = 10000;

    void Start () {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        TestNewBounds();
        sw.Stop();
        UnityEngine.Debug.Log(sw.ElapsedMilliseconds);
    }

    void TestOldBounds () {
        float t = 0;
        for(int i = 0; i < iterations; i++) {
            Bounds b1 = new Bounds() {min = Vector2.zero, max = Vector2.one};
            Ray2D ray = new Ray2D(new Vector2(-1f, 1f), new Vector2(1f, -0.25f).normalized);
            if(b1.IntersectRay(new Ray(ray.origin, ray.direction), out float dist)) {
                t = dist;
            }
        }
        UnityEngine.Debug.Log(t);
    }

    void TestNewBounds () {
        float t = 0;
        for(int i = 0; i < iterations; i++) {
            Bounds2D b1 = new Bounds2D(Vector2.zero, Vector2.one);
            Vector2 origin = new Vector2(-1f, 1f);
            Vector2 invDir = new Vector2(1f, -0.25f).normalized;
            invDir = new Vector2(1f / invDir.x, 1f / invDir.y);
            if(b1.IntersectRay(origin, invDir, out float dist)) {
                t = dist;
            }
        }
        UnityEngine.Debug.Log(t);
    }
}
