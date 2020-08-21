using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System;
using System.Threading;

public enum TerrainLayers {
    Ground,
    WaterSurface,
    Background,
    WaterBackground,
    Overlay
}

public struct MeshData {
    public List<Vector3> verts;
    public List<int> tris;
    public List<Vector3> uvs;
    public List<Vector2> animUVs;
    public int faceOffset;

    public void Initiate () {
        verts = new List<Vector3>(1024);
        tris = new List<int>(6144);
        uvs = new List<Vector3>(1024);
        animUVs = new List<Vector2>(1024);
        faceOffset = 0;
    }

    public void Clear () {
        verts.Clear();
        tris.Clear();
        uvs.Clear();
        animUVs.Clear();
        faceOffset = 0;
    }

    public void Apply (Mesh mesh, int subMesh = 0) {
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, subMesh);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, animUVs);
    }
}

public class TileString {
    public string nspace;
    public string id;

    public TileString () { }

    public TileString (string nspace, string id) {
        this.nspace = nspace;
        this.id = id;
    }
}

[System.Serializable]
public struct ChunkLoadBounds {
    public Vector2Int minimum {
        set; get;
    }
    public Vector2Int maximum {
        set; get;
    }

    public ChunkLoadBounds (Vector2Int minimum, Vector2Int maximum) {
        this.minimum = minimum;
        this.maximum = maximum;
    }

    public override string ToString ()
        => $"({minimum}, {maximum})";

    public bool IsAreaNull ()
        => ((maximum.x - minimum.x) * (maximum.y - minimum.y)) <= 0f;

    public static bool operator == (ChunkLoadBounds a, ChunkLoadBounds b)
        => a.minimum == b.minimum && a.maximum == b.maximum;

    public static bool operator != (ChunkLoadBounds a, ChunkLoadBounds b)
        => !(a == b);

    public override bool Equals (object obj) {
        if(!(obj is ChunkLoadBounds)) {
            return false;
        }

        var bounds = (ChunkLoadBounds)obj;
        return minimum.Equals(bounds.minimum) &&
               maximum.Equals(bounds.maximum);
    }

    public override int GetHashCode () {
        var hashCode = -1421831408;
        hashCode = hashCode * -1521134295 + base.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector2Int>.Default.GetHashCode(minimum);
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector2Int>.Default.GetHashCode(maximum);
        return hashCode;
    }

    public static ChunkLoadBounds BoundsFromRadius (Vector2Int centerChunkPos, Vector2Int loadRadius) {
        return new ChunkLoadBounds(
            new Vector2Int(
                centerChunkPos.x - loadRadius.x,
                centerChunkPos.y - loadRadius.y),
            new Vector2Int(
                centerChunkPos.x + loadRadius.x + 1,
                centerChunkPos.y + loadRadius.y + 1)
        );
    }
}

public class ChunkLoadCounter {
    public Vector2Int position;
    public HashSet<ulong> loaders;
    public Dictionary<ulong, float> lastSendTime;
    public CancellableTimer timer;

    public ChunkLoadCounter (Vector2Int position) {
        this.position = position;
        loaders = new HashSet<ulong>();
        lastSendTime = new Dictionary<ulong, float>();
    }
}

public class CancellableTimer {
    public bool isRunning {
        private set; get;
    }

    bool cancelled = false;
    Action callback;
    float seconds;

    public void Start (float seconds, Action callback) {
        this.seconds = seconds;
        this.callback = callback;
        isRunning = true;
        TerrainManager.inst.StartCoroutine(WaitForEndOfTimer());
    }

    public void Cancel () {
        cancelled = true;
    }

    IEnumerator WaitForEndOfTimer () {
        yield return new WaitForSeconds(seconds);
        if(!cancelled) {
            callback?.Invoke();
        }
        isRunning = false;
    }
}


#region Job System
public class ThreadedJobManager {
    public JobState targetLoadState = JobState.Unloaded;
    public List<ThreadedJob> jobs;

    public ThreadedJobManager () {
        jobs = new List<ThreadedJob>(1);
    }

    public virtual void StartNewJob (JobState newTargetLoadState, bool isReadonly, Action job, Action callback, Action cancelCallback, bool runImidialty) {
        if(newTargetLoadState != JobState.Saving) {
            targetLoadState = newTargetLoadState;
        }
    }

    public virtual void ForceLoad () {
        targetLoadState = JobState.Loaded;
    }

    public virtual void ForceUnload () {
        targetLoadState = JobState.Unloaded;
        CancelAllJobs();
    }

    public virtual void RemoveJob (ThreadedJob job) {
        jobs.Remove(job);
    }

    public virtual void CancelAllJobs () {
        foreach(ThreadedJob job in jobs) {
            job.Cancel();
        }
    }
}

public class ChunkJobManager : ThreadedJobManager {
    public Vector2Int position;

    public ChunkJobManager (Vector2Int position) : base() {
        this.position = position;
    }
    
    public override void StartNewJob (JobState newTargetLoadState, bool isReadonly, Action job, Action callback, Action cancelCallback, bool runImidialty) {
        base.StartNewJob(newTargetLoadState, isReadonly, job, callback, cancelCallback, runImidialty);
        
        if(jobs.Count > 0) {
            ThreadedJob newJob = new ThreadedJob(job, callback, cancelCallback, this, newTargetLoadState, isReadonly);

            // Work out if a cancel is needed.
            // Don't cancel jobs that do not have the same isReadonly value
            // Wait for unload to start a load (Before the file is being edited and we should wait for it to not be)
            // Cancel a load to start an unload
            // If there's two unload, cancel the new unload
            // If there's two load, cancel the new load
            bool onlyOtherReadMode = true;
            for(int i = 0; i < jobs.Count; i++) {
                if(jobs[i].isReadonly == isReadonly) {
                    onlyOtherReadMode = false;

                    if(jobs[i].IsCancelled) {
                        continue;
                    } else if(jobs[i].loadState == JobState.Unloaded && newTargetLoadState == JobState.Loaded) {
                        jobs.Add(newJob);
                        jobs[i].SetChainedJob(newJob);
                        break;
                    } else if(jobs[i].loadState == JobState.Loaded && newTargetLoadState == JobState.Unloaded) {
                        jobs[i].Cancel();
                        jobs.Add(newJob);
                        newJob.Run();
                        break;
                    } else if(jobs[i].loadState == JobState.Saving && newTargetLoadState == JobState.Loaded) {
                        Debug.LogError("Load request for a chunk being saved?");
                        break;
                    } else if(jobs[i].loadState == JobState.Saving && newTargetLoadState == JobState.Unloaded) { // The chunk is already getting saved.
                        jobs.Add(newJob);
                        jobs[i].SetChainedJob(newJob);
                    } else if(jobs[i].loadState == JobState.Loaded && newTargetLoadState == JobState.Saving) { // No need to save if it isn't even loaded
                        break;
                    } else if(jobs[i].loadState == JobState.Unloaded && newTargetLoadState == JobState.Saving) { // No need to save if it is unloading, unloading already saves
                        break;
                    } else if(jobs[i].loadState == newTargetLoadState) {
                        break;
                    } else {
                        break;
                    }
                }
            }
            if(onlyOtherReadMode) {
                Debug.Log("Only in other read mode");
                jobs.Add(newJob);
                if(TerrainManager.inst == null || runImidialty) {
                    newJob.Run();
                } else {
                    TerrainManager.inst.EnqueueJobToRun(newJob);
                }
            }
        } else {
            ThreadedJob newJob = new ThreadedJob(job, callback, cancelCallback, this, newTargetLoadState, isReadonly);
            jobs.Add(newJob);
            if(TerrainManager.inst == null || runImidialty) {
                newJob.Run();
            } else {
                TerrainManager.inst.EnqueueJobToRun(newJob);
            }
        }
    }

    public override void ForceUnload () {
        base.ForceUnload();
        if(jobs.Count == 0) {
            TerrainManager.inst.RemoveJobManager(position);
        }
    }

    public override void RemoveJob (ThreadedJob job) {
        base.RemoveJob(job);
        if(jobs.Count == 0 && targetLoadState == JobState.Unloaded) {
            TerrainManager.inst.RemoveJobManager(Hash.longFrom2D(position));
        }
    }
}

public class EntityJobManager : ThreadedJobManager {
    public int uid;

    public EntityJobManager (int uid) : base() {
        this.uid = uid;
    }

    public override void StartNewJob (JobState newTargetLoadState, bool isReadonly, Action job, Action callback, Action cancelCallback, bool runImidialty) {
        base.StartNewJob(newTargetLoadState, isReadonly, job, callback, cancelCallback, runImidialty);

        if(jobs.Count > 0) {
            ThreadedJob newJob = new ThreadedJob(job, callback, cancelCallback, this, newTargetLoadState, isReadonly);

            bool onlyOtherReadMode = true;
            for(int i = 0; i < jobs.Count; i++) {
                if(jobs[i].isReadonly == isReadonly) {
                    onlyOtherReadMode = false;

                    if(jobs[i].IsCancelled) {
                        continue;
                    } else if(jobs[i].loadState == JobState.Unloaded && newTargetLoadState == JobState.Loaded) {
                        jobs.Add(newJob);
                        jobs[i].SetChainedJob(newJob);
                        break;
                    } else if(jobs[i].loadState == JobState.Loaded && newTargetLoadState == JobState.Unloaded) {
                        jobs[i].Cancel();
                        jobs.Add(newJob);
                        newJob.Run();
                        break;
                    } else if(jobs[i].loadState == JobState.Saving && newTargetLoadState == JobState.Loaded) {
                        Debug.LogError("Load request for a chunk being saved?");
                        break;
                    } else if(jobs[i].loadState == JobState.Saving && newTargetLoadState == JobState.Unloaded) { // The chunk is already getting saved.
                        jobs.Add(newJob);
                        jobs[i].SetChainedJob(newJob);
                    } else if(jobs[i].loadState == JobState.Loaded && newTargetLoadState == JobState.Saving) { // No need to save if it isn't even loaded
                        break;
                    } else if(jobs[i].loadState == JobState.Unloaded && newTargetLoadState == JobState.Saving) { // No need to save if it is unloading, unloading already saves
                        break;
                    } else if(jobs[i].loadState == newTargetLoadState) {
                        break;
                    } else {
                        break;
                    }
                }
            }
            if(onlyOtherReadMode) {
                jobs.Add(newJob);
                if(TerrainManager.inst == null || runImidialty) {
                    newJob.Run();
                } else {
                    TerrainManager.inst.EnqueueJobToRun(newJob);
                }
            }
        } else {
            ThreadedJob newJob = new ThreadedJob(job, callback, cancelCallback, this, newTargetLoadState, isReadonly);
            jobs.Add(newJob);
            if(TerrainManager.inst == null || runImidialty) {
                newJob.Run();
            } else {
                TerrainManager.inst.EnqueueJobToRun(newJob);
            }
        }
    }

    public override void ForceUnload () {
        base.ForceUnload();
        if(jobs.Count == 0) {
            EntityManager.inst.RemoveJobManager(uid);
        }
    }

    public override void RemoveJob (ThreadedJob job) {
        base.RemoveJob(job);
        if(jobs.Count == 0 && targetLoadState == JobState.Unloaded) {
            EntityManager.inst.RemoveJobManager(uid);
        }
    }

    public override void CancelAllJobs () {
        base.CancelAllJobs();
        EntityManager.inst.RemoveJobManager(uid);
    }
}

public class ThreadedJob {
    public bool isReadonly;
    public JobState loadState {
        get; private set;
    }
    ThreadedJobManager jobManager;
    bool cancelFlag = false;
    bool isRunning = false;
    bool isComplete = false;

    Action job;
    Action cancelCallback;
    Action callback;
    ThreadedJob executeOnDone;

    public ThreadedJob (Action job, Action callback, Action cancelCallback, ThreadedJobManager jobManager, JobState loadState, bool isReadonly) {
        this.job = job;
        this.callback = callback;
        this.cancelCallback = cancelCallback;
        this.jobManager = jobManager;
        this.loadState = loadState;
        this.isReadonly = isReadonly;
    }

    public void SetChainedJob (ThreadedJob executeOnDone) {
        if(isComplete) {
            Debug.LogError("The chained job will never run.");
        }
        this.executeOnDone = executeOnDone;
    }

    public void Run () {
        if(IsCancelled) {
            RunCallbacks(true);
            return;
        }

        isRunning = true;
        ThreadPool.QueueUserWorkItem((n) => {
            try {
                job?.Invoke();
            } catch (Exception e) {
                throw e;
            }
            //Thread.Sleep(500);
            TerrainManager.inst.EnqueueMainThreadCallbacks(() => {
                isRunning = false;
                RunCallbacks(cancelFlag);
            });
        });
    }

    public void RunCallbacks (bool doCancel) {
        cancelFlag = doCancel;
        isComplete = true;
        if(cancelFlag) {
            cancelCallback?.Invoke();
        } else {
            callback?.Invoke();
        }
        jobManager.RemoveJob(this);
        executeOnDone?.Run();
    }

    public void Cancel () {
        cancelFlag = true;
        if(!isRunning) {
            jobManager.RemoveJob(this);
        }
    }

    public bool IsCancelled {
        get {
            return cancelFlag;
        }
    }

    public bool IsRunning () {
        return isRunning;
    }
}

public enum JobState {
    Loaded,
    Unloaded,
    Saving
}
#endregion


[Serializable]
public struct Bounds2D {
    public Vector2 min;
    public Vector2 max;

    public Vector2 center {
        get {
            return new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
        }
    }

    public Vector2 size {
        get {
            return new Vector2(max.x - min.x, max.y - min.y);
        }
    }

    public Bounds2D (Vector2 min, Vector2 max) {
        this.min = min;
        this.max = max;
    }

    public Bounds2D CreateBoundsFromPoints (Vector2 p1, Vector2 p2) {
        return new Bounds2D(
            new Vector2(
                Mathf.Min(p1.x, p2.x),
                Mathf.Min(p1.y, p2.y)),
            new Vector2(
                Mathf.Max(p1.x, p2.x),
                Mathf.Max(p1.y, p2.y))
        );
    }

    public bool Overlaps (Bounds2D b) {
        return (this.min.x <= b.max.x && b.min.x <= this.max.x &&
                this.min.y <= b.max.y && b.min.y <= this.max.y);
    }

    public bool Overlaps (Vector2 p) {
        return (p.x > min.x && p.x < max.x && p.y > min.y && p.y < max.y);
    }

    public bool IntersectRay (Vector2 origin, Vector2 invDir, out float distance) {
        bool signDirX = invDir.x < 0;
        bool signDirY = invDir.y < 0;

        Vector2 bbox = signDirX ? max : min;
        float tmin = (bbox.x - origin.x) * invDir.x;
        bbox = signDirX ? min : max;
        float tmax = (bbox.x - origin.x) * invDir.x;
        bbox = signDirY ? max : min;
        float tymin = (bbox.y - origin.y) * invDir.y;
        bbox = signDirY ? min : max;
        float tymax = (bbox.y - origin.y) * invDir.y;

        if((tmin > tymax) || (tymin > tmax)) {
            distance = 0f;
            return false;
        }
        if(tymin > tmin) {
            tmin = tymin;
        }
        if(tymax < tmax) {
            tmax = tymax;
        }

        distance = tmin;
        return true;
    }

    public void ExtendByDelta (Vector2 delta) {
        if(delta.x < 0f) {
            min.x += delta.x;
        } else {
            max.x += delta.x;
        }
        if(delta.y < 0f) {
            min.y += delta.y;
        } else {
            max.y += delta.y;
        }
    }

    public void ExtendByDelta (float deltaX, float deltaY) {
        if(deltaX < 0f) {
            min.x += deltaX;
        } else {
            max.x += deltaX;
        }
        if(deltaY < 0f) {
            min.y += deltaY;
        } else {
            max.y += deltaY;
        }
    }

    public void PositionToTile (int tileX, int tileY) {
        min.x += tileX;
        min.y += tileY;
        max.x += tileX;
        max.y += tileY;
    }

    public void Move (Vector2 delta) {
        min.x += delta.x;
        min.y += delta.y;
        max.x += delta.x;
        max.y += delta.y;
    }

    public Vector2 GetIntersectingArea (Bounds2D b) {
        return new Vector2(
            Mathf.Max(Mathf.Min(max.x, b.max.x) - Mathf.Max(min.x, b.min.x), 0f),
            Mathf.Max(Mathf.Min(max.y, b.max.y) - Mathf.Max(min.y, b.min.y), 0f)
        );
    }
}
