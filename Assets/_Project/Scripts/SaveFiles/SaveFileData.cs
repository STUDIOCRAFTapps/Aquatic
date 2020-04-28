using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveFileData {
    public string fileName;
    public string fullName;
    public int aikonId;

    public ushort shellsCollected;
    public ushort shellsTotal;
    public ushort goldenShellsCollected;
    public ushort goldenShellsTotal;

    public byte currentChaptre;
}
