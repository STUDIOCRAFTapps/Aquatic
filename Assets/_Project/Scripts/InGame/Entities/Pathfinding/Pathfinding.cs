using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Debug = UnityEngine.Debug;


// This class isn't a monobehaviour, meaning you'll have to store an instance of this class somewhere,
// I recommend storing it in the PathRequestManager class
public class Pathfinder {

    int maxNodeCount;
    int maxSeekDistance;
    Queue<NodeDataStructure> nodeDataStructureQueue;
    
    public Pathfinder (int maxNodeCount, int maxSeekDistance) {
        nodeDataStructureQueue = new Queue<NodeDataStructure>();
        this.maxNodeCount = maxNodeCount;
        this.maxSeekDistance = maxSeekDistance;
    }

    #region Requests
    /// <summary>
    /// Will request a path and ask all the points in the path as a result
    /// </summary>
    public void FindPath (PathRequest request, Action<PathResult> callback, Vector2Int seekingSize) {
        I_FindPath(request, callback, true, seekingSize);
    }

    /// <summary>
    /// Will request a path and ask for only the next point to follow to reach the endpoint.
    /// It is recomanded to use this function instead of "FindPath" since this one does not allocate
    /// a single byte.
    /// </summary>
    public void FindNextPoint (PathRequest request, Action<NextPointResult> callback, Vector2Int seekingSize) {
        I_FindPath(request, callback, false, seekingSize);
    }
    #endregion

    #region Pathfinding
    void I_FindPath (PathRequest request, object callback, bool getCompletePath, Vector2Int size) {

        bool doSizeCheck = size != Vector2Int.one;

        // Creates a stopwatch to check the performance of the pathfinding algorithm.
        Stopwatch sw = new Stopwatch();
        sw.Start();

        bool pathSuccess = false;
        
        // Stores reference for the starting and ending nodes
        ReadOnlyNode roStartNode = PathgridManager.inst.GetReadOnlyNodeFromTilePoint(Mathf.FloorToInt(request.pathStart.x), Mathf.FloorToInt(request.pathStart.y));
        ReadOnlyNode roEndNode = PathgridManager.inst.GetReadOnlyNodeFromTilePoint(Mathf.FloorToInt(request.pathEnd.x), Mathf.FloorToInt(request.pathEnd.y));
        Node startNode;
        Node targetNode;

        // Gets a new node data structure from the queue. This class store all the utilities a class need
        // for the algorithm to run independetly on a thread. It includes the heaps, the hash sets and
        // other reused object, such as the neighbour list
        NodeDataStructure nds = GetNewNodeDataStructure();

        #region A* Algorithm
        if(roStartNode.walkable && roEndNode.walkable) {
            startNode = nds.GetNodeFromTilePoint(Vector2Int.FloorToInt(request.pathStart));
            targetNode = nds.GetNodeFromTilePoint(Vector2Int.FloorToInt(request.pathEnd));

            startNode.parent = startNode;
            nds.nodeHeap.Add(startNode);

            int limit = maxNodeCount;
            while(nds.nodeHeap.Count > 0) {
                Node currentNode = nds.nodeHeap.RemoveFirst();
                nds.nodeHashSet.Add(currentNode);

                if(currentNode == targetNode) {
                    sw.Stop();
                    pathSuccess = true;
                    break;
                }

                nds.GetNeighbourTiles(currentNode);
                for(int n = 0; n < nds.neighbours.Count; n++) {
                    Node neighbour = nds.neighbours[n];
                    // If it doesn't even exist, check the next neighbour
                    if(neighbour == null) {
                        continue;
                    }
                    // If you can't walk to it or you've already walked at it, that's a big no no
                    if(!neighbour.readOnly.walkable || nds.nodeHashSet.Contains(neighbour)) {
                        continue;
                    }
                    // If neighbour is a corner and at least one of the side tile aren't walkable, you can't go diagonally.
                    if(neighbour.isNeighbourCorner && !(nds.neighbours[Modulo(n - 1, 8)].readOnly.walkable && nds.neighbours[Modulo(n + 1, 8)].readOnly.walkable)) { //Is broken with size checks now
                        continue;
                    }
                    if(doSizeCheck) {
                        if(!nds.AreAllTilesWalkable(neighbour.readOnly.gridX, neighbour.readOnly.gridY, size)) {
                            continue;
                        }
                    }

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode.readOnly, neighbour.readOnly) + neighbour.readOnly.penalty;
                    if(newMovementCostToNeighbour < neighbour.gCost || !nds.nodeHeap.Contains(neighbour)) {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour.readOnly, targetNode.readOnly);
                        neighbour.parent = currentNode;

                        if(neighbour.hCost > maxSeekDistance) {
                            continue;
                        }

                        if(!nds.nodeHeap.Contains(neighbour)) {
                            nds.nodeHeap.Add(neighbour);
                        } else {
                            nds.nodeHeap.UpdateItem(neighbour);
                        }
                    }
                }

                limit--;
                if(limit < 0) {
                    break;
                }
            }
        } else {
            startNode = new Node() {readOnly = roStartNode};
            targetNode = new Node() {readOnly = roEndNode};
        }
        #endregion

        // Retraces the path, filling the proper variable with the choosen output data type (Next point or path)
        List<Vector2> waypoints = null;
        Vector2 onlyPoint = Vector2.zero;
        if(pathSuccess) {
            if(getCompletePath) {
                waypoints = new List<Vector2>();
                RetracePath(ref nds, ref waypoints, startNode, targetNode, size);
                pathSuccess = waypoints.Count > 0;
            } else {
                onlyPoint = RetracePathOnePoint(ref nds, startNode, targetNode, size);
            }
        }

        // Dispose the nodeDataStructure for future uses
        lock(nodeDataStructureQueue) {
            nodeDataStructureQueue.Enqueue(nds);
        }

        // Executes the proper callback
        if(getCompletePath) {
            ((Action<PathResult>)callback)(new PathResult(waypoints, pathSuccess, (Action<List<Vector2>, bool>)request.callbacks));
        } else {
            ((Action<NextPointResult>)callback)(new NextPointResult(onlyPoint, pathSuccess, (Action<Vector2, bool>)request.callbacks));
        }
    }

    static void RetracePath (ref NodeDataStructure nds, ref List<Vector2> waypoints, Node startNode, Node endNode, Vector2 size) {
        Node currentNode = endNode;

        while(currentNode != startNode) {
            waypoints.Insert(0, currentNode.readOnly.worldPosition + (size * 0.5f));
            currentNode = currentNode.parent;
        }
    }

    static Vector2 RetracePathOnePoint (ref NodeDataStructure nds, Node startNode, Node endNode, Vector2 size) {
        Node currentNode = endNode;
        Node previousNode = currentNode;

        while(currentNode != startNode) {
            previousNode = currentNode;
            currentNode = currentNode.parent;
        }
        return previousNode.readOnly.worldPosition + (size * 0.5f);
    }
    #endregion

    #region Pools
    NodeDataStructure GetNewNodeDataStructure () {
        lock(nodeDataStructureQueue) {
            if(nodeDataStructureQueue.Count > 0) {
                NodeDataStructure nodeDataS = nodeDataStructureQueue.Dequeue();
                nodeDataS.Clear();
                return nodeDataS;
            }
        }
        return new NodeDataStructure(maxNodeCount);
    }
    #endregion

    #region Utils
    static int GetDistance (ReadOnlyNode nodeA, ReadOnlyNode nodeB) {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if(dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    static int Modulo (int x, int m) {
        int r = x % m;
        return r < 0 ? r + m : r;
    }
    #endregion
}

public class NodeDataStructure {
    public Heap<Node> nodeHeap;
    public HashSet<Node> nodeHashSet;
    public Dictionary<long, Node> nodes;
    public List<Node> neighbours;
    Queue<Node> unusedNodes;

    public NodeDataStructure (int maxNodeCount) {
        nodeHeap = new Heap<Node>(maxNodeCount);
        nodeHashSet = new HashSet<Node>();
        unusedNodes = new Queue<Node>();
        nodes = new Dictionary<long, Node>();
        neighbours = new List<Node>(8);

        for(int i = 0; i < maxNodeCount; i++) {
            unusedNodes.Enqueue(new Node());
        }
    }

    public void Clear () {
        nodeHeap.Clear();
        nodeHashSet.Clear();
        neighbours.Clear();

        foreach(KeyValuePair<long, Node> kvp in nodes) {
            unusedNodes.Enqueue(kvp.Value);
        }
        nodes.Clear();
    }

    public Node GetNodeFromTilePoint (Vector2Int position) {
        if(nodes.TryGetValue(Hash.hVec2Int(position), out Node node)) {
            return node;
        } else {
            if(unusedNodes.Count > 0) {
                ReadOnlyNode ron = PathgridManager.inst.GetReadOnlyNodeFromTilePoint(position.x, position.y);
                if(ron == null) {
                    return null;
                }

                Node newNode = unusedNodes.Dequeue();
                newNode.HeapIndex = 0;
                newNode.gCost = 0;
                newNode.hCost = 0;
                newNode.readOnly = ron;
                nodes.Add(Hash.hVec2Int(position), newNode);
                return newNode;
            } else {
                return null;
            }
        }
    }

    public bool AreAllTilesWalkable (int posX, int posY, Vector2Int size) {
        for(int x = posX; x < posX + size.x; x++) {
            for(int y = posY; y < posY + size.y; y++) {
                if(!(x == posX && y == posY)) {
                    if(!PathgridManager.inst.GetReadOnlyNodeFromTilePoint(x, y).walkable)
                        return false;
                }
            }
        }
        return true;
    }

    static private readonly int[] nX = new int[] { -1, 0, 1, 1, 1, 0, -1, -1 };
    static private readonly int[] nY = new int[] { 1, 1, 1, 0, -1, -1, -1, 0 };
    static private readonly bool[] nC = new bool[] { true, false, true, false, true, false, true, false};

    // Orders neighbours clockwise
    public void GetNeighbourTiles (Node node) {
        neighbours.Clear();
        for(int n = 0; n < 8; n++) {
            neighbours.Add(GetNodeFromTilePoint(new Vector2Int(node.readOnly.gridX + nX[n], node.readOnly.gridY + nY[n])));
            neighbours[neighbours.Count - 1].isNeighbourCorner = nC[n];
        }
    }
}
