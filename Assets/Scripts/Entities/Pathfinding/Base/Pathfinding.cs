using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Debug = UnityEngine.Debug;

public class Pathfinder {

    int maxNodeCount;
    Queue<NodeDataStructure> nodeDataStructureQueue;

    public Pathfinder (int maxNodeCount) {
        nodeDataStructureQueue = new Queue<NodeDataStructure>();
        this.maxNodeCount = maxNodeCount;
    }

    #region Pathfinding
    public void FindPath (PathRequest request, Action<PathResult> callback) {

        Stopwatch sw = new Stopwatch();
        sw.Start();

        List<Vector2> waypoints = new List<Vector2>();
        bool pathSuccess = false;
        
        ReadOnlyNode roStartNode = PathgridManager.inst.GetReadOnlyNodeFromTilePoint(Mathf.FloorToInt(request.pathStart.x), Mathf.FloorToInt(request.pathStart.y));
        ReadOnlyNode roEndNode = PathgridManager.inst.GetReadOnlyNodeFromTilePoint(Mathf.FloorToInt(request.pathEnd.x), Mathf.FloorToInt(request.pathEnd.y));
        Node startNode;
        Node targetNode;

        if(roStartNode.walkable && roEndNode.walkable) {
            NodeDataStructure nds = GetNewNodeDataStructure();

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
                foreach(Node neighbour in nds.neighbours) {
                    if(neighbour == null) {
                        limit = 0;
                        break;
                    }
                    if(!neighbour.readOnly.walkable || nds.nodeHashSet.Contains(neighbour)) {
                        continue;
                    }

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode.readOnly, neighbour.readOnly) + neighbour.readOnly.penalty;
                    if(newMovementCostToNeighbour < neighbour.gCost || !nds.nodeHeap.Contains(neighbour)) {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour.readOnly, targetNode.readOnly);
                        neighbour.parent = currentNode;

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
            nodeDataStructureQueue.Enqueue(nds);
        } else {
            startNode = new Node() {readOnly = roStartNode};
            targetNode = new Node() {readOnly = roEndNode};
        }
        if(pathSuccess) {
            RetracePath(ref waypoints, startNode, targetNode);
            pathSuccess = waypoints.Count > 0;
        }
        callback(new PathResult(waypoints, pathSuccess, request.callbacks));

    }

    static void RetracePath (ref List<Vector2> waypoints, Node startNode, Node endNode) {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while(currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        for(int i = path.Count - 1; i >= 0; i--) {
            waypoints.Add(path[i].readOnly.worldPosition + (Vector3)(Vector2.one * 0.5f));
        }

    }

    static int GetDistance (ReadOnlyNode nodeA, ReadOnlyNode nodeB) {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if(dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
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

    public void GetNeighbourTiles (Node node) {
        neighbours.Clear();
        for(int x = -1; x <= 1; x++) {
            for(int y = -1; y <= 1; y++) {
                if(x == 0 && y == 0)
                    continue;

                neighbours.Add(GetNodeFromTilePoint(new Vector2Int(node.readOnly.gridX + x, node.readOnly.gridY + y)));
            }
        }
    }
}
