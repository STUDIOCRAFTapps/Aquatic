using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveFileData {
    public string folderName;
    public string fullName;
    public int aikonId;

    public ushort shellsCollected;
    public ushort shellsTotal;
    public ushort goldenShellsCollected;
    public ushort goldenShellsTotal;

    public byte currentChaptre;

    public byte mainVersionNumber;
    public byte secondVersionNumber;
    public byte bugfixVersionNumber;
}
