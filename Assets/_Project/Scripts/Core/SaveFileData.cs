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
    public int aikanId;
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
    public byte[] clientProtectionSalt;


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
    static public readonly string infoFile = "saveInfo.json";
    static private JsonSerializerSettings jss = new JsonSerializerSettings() {
        TypeNameHandling = TypeNameHandling.Auto,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public void Save () {
        if(string.IsNullOrEmpty(folderDirectory)) {
            throw new System.Exception("Save file's folder is missing. Please link or generate.");
        }

        string filePath = Path.Combine(folderDirectory, infoFile);

        using(StreamWriter file = File.CreateText(filePath)) {
            JsonSerializer serializer = JsonSerializer.Create(jss);
            serializer.Serialize(file, this);
        }
    }

    public static SaveFileData Load (string saveFolder) {
        string filePath = Path.Combine(saveFolder, infoFile);
        if(File.Exists(filePath)) {
            using(StreamReader file = File.OpenText(filePath)) {
                JsonSerializer serializer = JsonSerializer.Create(jss);
                return (SaveFileData)serializer.Deserialize(file, typeof(SaveFileData));
            }
        } else {
            SaveFileData newSaveFileData = new SaveFileData() {
                fullName = "Unnamed World",
                folderDirectory = saveFolder,
                folderName = new DirectoryInfo(saveFolder).Name,
                welcomeMessage = "Looks like this world didn\'t have any safe info file. " +
                "A new one was created, you can refill yourself the missing data as you please in the json file."
            };
            newSaveFileData.Save();
            return newSaveFileData;
        }
    }
    #endregion

    public SaveFileData ShallowCopy () {
        return (SaveFileData)MemberwiseClone();
    }

    public void CreateBackup () {
        FileUtils.CompressDirectory(
            folderDirectory, 
            Path.Combine(backupDirectory,$"{folderName}_{System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}"),
            (progress) => {
                Debug.Log(progress);
            }
        );
    }

    public void LoadBackup (string filePath) {
        Directory.Delete(folderDirectory, true);
        FileUtils.DecompressToDirectory(
            filePath, 
            folderDirectory,
            (progress) => {
                Debug.Log(progress);
            }
        );
    }

    public void Delete () {
        Directory.Delete(folderDirectory, true);
    }

    public void DeletePlayerData () {
        Directory.Delete(Path.Combine(folderDirectory,"player_data"), true);
    }

    public void DeletePlayData () {
        Directory.Delete(Path.Combine(folderDirectory, "play"), true);
    }

    public void Duplicate () {
        string folderDirectoryCopy = $"{folderDirectory}_copy_{System.DateTime.Now.ToString("yyyyMMddHHmmss")}";
        FileUtils.DirectoryCopy(folderDirectory, folderDirectoryCopy, true);

        SaveFileData saveFileDataCopy = Load(folderDirectoryCopy);
        saveFileDataCopy.folderDirectory = folderDirectoryCopy;
        saveFileDataCopy.folderName = new DirectoryInfo(folderDirectoryCopy).Name;
        saveFileDataCopy.backupDirectory = Path.Combine(SavesMenuManager.inst.allBackupsPath, saveFileDataCopy.folderName);
        Directory.CreateDirectory(saveFileDataCopy.backupDirectory);
        saveFileDataCopy.fullName += " Duplicate";
        saveFileDataCopy.Save();

        SavesMenuManager.inst.LoadSaveDisplay(folderDirectoryCopy);
    }
}
