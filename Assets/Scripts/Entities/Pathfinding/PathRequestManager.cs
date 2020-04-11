using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

public class PathRequestManager : MonoBehaviour {

    public int maxNodeCount = 1024;

    public static PathRequestManager inst;
    Pathfinder pathfinder;
    Queue<PathResult> results = new Queue<PathResult>();

    void Awake () {
        inst = this;
        pathfinder = new Pathfinder(maxNodeCount);
    }

    void Update () {
        if(results.Count > 0) {
            int itemsInQueue = results.Count;
            lock(results) {
                for(int i = 0; i < itemsInQueue; i++) {
                    PathResult result = results.Dequeue();
                    result.callbacks(result.path, result.success);
                }
            }
        }
    }

    public void RequestPath (PathRequest request) {
        ThreadPool.QueueUserWorkItem((job) => {
            pathfinder.FindPath(request, inst.FinishedProcessingPath);
        });
    }

    public void FinishedProcessingPath (PathResult result) {
        lock(results) {
            results.Enqueue(result);
        }
    }
}

public struct PathResult {
    public bool success;
    public List<Vector2> path;
    public Action<List<Vector2>, bool> callbacks;

    public PathResult (List<Vector2> path, bool success, Action<List<Vector2>, bool> callbacks) {
        this.path = path;
        this.success = success;
        this.callbacks = callbacks;
    }
}

public struct PathRequest {
    public Vector2 pathStart;
    public Vector2 pathEnd;
    public Action<List<Vector2>, bool> callbacks;

    public PathRequest (Vector2 pathStart, Vector2 pathEnd, Action<List<Vector2>, bool> callbacks) {
        this.pathStart = pathStart;
        this.pathEnd = pathEnd;
        this.callbacks = callbacks;
    }
}
