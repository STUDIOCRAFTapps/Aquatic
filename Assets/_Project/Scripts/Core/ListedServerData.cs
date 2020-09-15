using System.Text;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class ListedServerData {
    // Technical
    public string fileName;
    public string fileDirectory;

    // Presentation
    public string fullName = "A new server";

    // Multiplayer
    public string ip = "127.0.0.1";
    public ushort httpPort = 7777;

    #region Folder Names
    public const string authorizedCharsString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

    public void GenerateFolderName (string listedServerFolderDirectory) {
        StringBuilder sb = new StringBuilder();
        foreach(char c in fullName) {
            if(authorizedCharsString.Contains(c)) {
                sb.Append(c);
            } else if(char.IsWhiteSpace(c)) {
                sb.Append('_');
            }
        }
        sb.Append(".json");

        fileName = sb.ToString();
        while(true) {
            if(!Directory.Exists(Path.Combine(listedServerFolderDirectory, fileName))) {
                break;
            } else {
                fileName += "_";
            }
        }
        fileDirectory = Path.Combine(listedServerFolderDirectory, fileName);
    }

    public void LoadFolderName (string listedServerFolderDirectory) {
        fileDirectory = Path.Combine(listedServerFolderDirectory, fileName);
    }
    #endregion

    #region Save / Load
    static private JsonSerializerSettings jss = new JsonSerializerSettings() {
        TypeNameHandling = TypeNameHandling.Auto,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public void Save () {

        using(StreamWriter file = File.CreateText(fileDirectory)) {
            JsonSerializer serializer = JsonSerializer.Create(jss);
            serializer.Serialize(file, this);
        }
    }

    public static ListedServerData Load (string fileDirectory) {
        using(StreamReader file = File.OpenText(fileDirectory)) {
            JsonSerializer serializer = JsonSerializer.Create(jss);
            return (ListedServerData)serializer.Deserialize(file, typeof(ListedServerData));
        }
    }

    public void Delete () {
        File.Delete(fileDirectory);
    }
    #endregion
}
