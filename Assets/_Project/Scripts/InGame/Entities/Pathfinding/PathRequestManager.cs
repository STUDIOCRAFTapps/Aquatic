using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PathRequestManager : MonoBehaviour {

    public int maxNodeCount = 1024;
    public int maxSeekDistance = 256;

    public static PathRequestManager inst;
    Pathfinder pathfinder;
    Queue<PathResult> pathResults = new Queue<PathResult>();
    Queue<NextPointResult> nextPointResults = new Queue<NextPointResult>();

    void Awake () {
        inst = this;
        pathfinder = new Pathfinder(maxNodeCount, maxSeekDistance);
    }

    void Update () {
        if(pathResults.Count > 0) {
            int itemsInQueue = pathResults.Count;
            lock(pathResults) {
                for(int i = 0; i < itemsInQueue; i++) {
                    PathResult result = pathResults.Dequeue();
                    result.callbacks(result.path, result.success);
                }
            }
        }
        if(nextPointResults.Count > 0) {
            int itemsInQueue = nextPointResults.Count;
            lock(nextPointResults) {
                for(int i = 0; i < itemsInQueue; i++) {
                    NextPointResult result = nextPointResults.Dequeue();
                    result.callbacks(result.nextPoint, result.success);
                }
            }
        }
    }

    public void RequestPath (PathRequest request, Vector2Int seekerSize) {
        ThreadPool.QueueUserWorkItem((job) => {
            pathfinder.FindPath(request, inst.FinishedProcessingPath, seekerSize);
        });
    }

    public void RequestNextPoint (PathRequest request, Vector2Int seekerSize) {
        ThreadPool.QueueUserWorkItem((job) => {
            pathfinder.FindNextPoint(request, inst.FinishedProcessingPath, seekerSize);
        });
    }

    public void FinishedProcessingPath (PathResult result) {
        lock(pathResults) {
            pathResults.Enqueue(result);
        }
    }

    public void FinishedProcessingPath (NextPointResult result) {
        lock(nextPointResults) {
            nextPointResults.Enqueue(result);
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

public struct NextPointResult {
    public bool success;
    public Vector2 nextPoint;
    public Action<Vector2, bool> callbacks;

    public NextPointResult (Vector2 pathPoint, bool success, Action<Vector2, bool> callbacks) {
        this.nextPoint = pathPoint;
        this.success = success;
        this.callbacks = callbacks;
    }
}

public struct PathRequest {
    public Vector2 pathStart;
    public Vector2 pathEnd;
    public object callbacks;

    public PathRequest (Vector2 pathStart, Vector2 pathEnd, object callbacks) {
        this.pathStart = pathStart;
        this.pathEnd = pathEnd;
        this.callbacks = callbacks;
    }
}
