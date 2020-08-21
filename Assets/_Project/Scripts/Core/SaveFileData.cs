using System.Text;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class SaveFileData {
    // Technical
    public string folderName;
    public string folderDirectory;
    public string backupDirectory;

    // Presentation
    public string fullName = "New world";
    public int aikonId;
    public bool editModeEnabled = true;

    // Progress
    public byte currentChaptre;
    public ushort shellsCollected;
    public ushort shellsTotal;
    public ushort goldenShellsCollected;
    public ushort goldenShellsTotal;

    // Verions
    public byte mainVersionNumber;
    public byte secondVersionNumber;
    public byte bugfixVersionNumber;

    // Multiplayer
    public string welcomeMessage = "Save the world, my final message.";
    public ushort httpPort = 7777;
    public bool startHostOnPlay = true;


    #region Folder Names
    public const string authorizedCharsString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

    public void GenerateFolderName (string savesFolderDirectory, string backupsFolderDirectory) {
        StringBuilder sb = new StringBuilder();
        foreach(char c in fullName) {
            if(authorizedCharsString.Contains(c)) {
                sb.Append(c);
            } else if(char.IsWhiteSpace(c)) {
                sb.Append('_');
            }
        }

        folderName = sb.ToString();
        while(true) {
            if(!Directory.Exists(Path.Combine(savesFolderDirectory, folderName))) {
                break;
            } else {
                folderName += "_";
            }
        }
        folderDirectory = Path.Combine(savesFolderDirectory, folderName);
        backupDirectory = Path.Combine(backupsFolderDirectory, folderName);

        if(!Directory.Exists(folderDirectory)) {
            Directory.CreateDirectory(folderDirectory);
        }
        if(!Directory.Exists(backupDirectory)) {
            Directory.CreateDirectory(backupDirectory);
        }
    }

    public void LoadFolderName (string savesFolderDirectory, string backupsFolderDirectory) {
        folderDirectory = Path.Combine(savesFolderDirectory, folderName);
        backupDirectory = Path.Combine(backupsFolderDirectory, folderName);

        if(!Directory.Exists(folderDirectory)) {
            Directory.CreateDirectory(folderDirectory);
        }
        if(!Directory.Exists(backupDirectory)) {
            Directory.CreateDirectory(backupDirectory);
        }
    }
    #endregion

    #region Save / Load
    static private readonly string infoFile = "worldInfo.wdat";
    static private JsonSerializerSettings jss = new JsonSerializerSettings() {
        TypeNameHandling = TypeNameHandling.Auto,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public void Save () {
        if(string.IsNullOrEmpty(folderDirectory)) {
            throw new System.Exception("Save file's folder is missing. Please link or generate.");
        }
        
        using(StreamWriter file = File.CreateText(folderDirectory)) {
            JsonSerializer serializer = JsonSerializer.Create(jss);
            serializer.Serialize(file, this);
        }
    }

    public static SaveFileData Load (string saveFolder) {
        using(StreamReader file = File.OpenRead(Path.Combine(saveFolder))) {
            JsonSerializer serializer = JsonSerializer.Create(jss);
            serializer.Serialize(file, this);
        }
    }
    #endregion
}
