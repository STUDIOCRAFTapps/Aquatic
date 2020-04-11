using UnityEngine;
using System.Collections;

public class ReadOnlyNode {
    public bool walkable;
    public Vector3 worldPosition;
    public int gridX;
    public int gridY;
    public int penalty;

    public ReadOnlyNode () {
    }

    public void SetData (bool walkable, Vector3 worldPosition, int gridX, int gridY, int penalty) {
        this.walkable = walkable;
        this.worldPosition = worldPosition;
        this.gridX = gridX;
        this.gridY = gridY;
        this.penalty = penalty;
    }
}

public class Node : IHeapItem<Node> {
    public ReadOnlyNode readOnly;

    public int gCost;
	public int hCost;
	public Node parent;
	int heapIndex;

    public Node () {
    }

	public int fCost {
		get {
			return gCost + hCost;
		}
	}

	public int HeapIndex {
		get {
			return heapIndex;
		}
		set {
			heapIndex = value;
		}
	}

	public int CompareTo(Node nodeToCompare) {
		int compare = fCost.CompareTo(nodeToCompare.fCost);
		if(compare == 0) {
			compare = hCost.CompareTo(nodeToCompare.hCost);
		}
		return -compare;
	}
}
