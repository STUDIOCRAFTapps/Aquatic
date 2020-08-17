using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using UnityEngine.UI;
using TMPro;
using MLAPI;

public class SavesMenuManager : MonoBehaviour {

    public Transform displayItemParent;
    public SaveFileDisplayItem saveFileDisplayPrefab;
    public Sprite[] aikansSprites;

    public TMP_InputField worldNameInputField;
    public Transform[] aikansSelection;
    public Color selectedAikanColor;
    public Color defaultAikanColor;

    public TMP_InputField serverIPField;

    public static SavesMenuManager inst;

    public const string savesFolder = "saves";
    public const string infoFile = "worldInfo.wdat";
    public const string authorizedCharsString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
    public static readonly string[] newNames = { "_new","_newest","_the_final_one","_newer","_version42","_the_only_one","_2" };

    public int selectedAikanID = 0;
    public int prevAikanUIID = 0;

    BinaryFormatter bf;
    StringBuilder sb;
    char s; // Separator char
    string datapath;
    string saveFileDirectory;

    private void Awake () {
        bool isNetworkManagerLoaded = false;
        int countLoaded = SceneManager.sceneCount;
        for(int i = 0; i < countLoaded; i++) {
            if(SceneManager.GetSceneAt(i).name == "NetworkManager") {
                isNetworkManagerLoaded = true;
            }
        }
        if(!isNetworkManagerLoaded) {
            SceneManager.LoadScene("NetworkManager", LoadSceneMode.Additive);
        }

        inst = this;

        s = Path.DirectorySeparatorChar;
        datapath = Application.persistentDataPath;
        sb = new StringBuilder();
        bf = new BinaryFormatter();

        saveFileDirectory = datapath + s + savesFolder;

        LoadAllSaveDisplay();
    }

    #region SaveFileDisplayLoaders
    void LoadAllSaveDisplay () {
        string[] allSavesDir = Directory.GetDirectories(saveFileDirectory);
        List<DirectoryInfo> allSaveDirInfo = new List<DirectoryInfo>();
        foreach(string dir in allSavesDir) {
            allSaveDirInfo.Add(new DirectoryInfo(dir));
        }
        allSaveDirInfo.OrderBy(f => f.LastAccessTimeUtc);
        
        for(int i = 0; i < allSaveDirInfo.Count; i++) {
            LoadSaveDisplay(allSaveDirInfo[i].FullName);
        }
    }

    public void LoadSaveDisplay (string saveFolderPath) {
        string saveFileDataDir = Path.Combine(saveFolderPath, infoFile);
        SaveFileData saveFileData = null;

        if(File.Exists(saveFileDataDir)) {
            using(FileStream fs = new FileStream(saveFileDataDir, FileMode.Open)) {
                try {
                    saveFileData = (SaveFileData)bf.Deserialize(fs);
                } catch(SerializationException e) {
                    Debug.LogError("Failed deserialization of save folders info file: " + e);
                }
            }
            if(saveFileData != null) {
                string folderName = new DirectoryInfo(saveFolderPath).Name;
                if(saveFileData.folderName != folderName) {
                    saveFileData.folderName = folderName;

                    using(FileStream fswrite = new FileStream(saveFileDataDir, FileMode.Create)) {
                        bf.Serialize(fswrite, saveFileData);
                    }
                }
            }
        } else {
            saveFileData = new SaveFileData() {
                aikonId = 0,
                currentChaptre = 0,
                folderName = new DirectoryInfo(saveFolderPath).Name,
                fullName = "New Save File",
                goldenShellsCollected = 0,
                goldenShellsTotal = 0,
                shellsCollected = 0,
                shellsTotal = 0
            };
            using(FileStream fs = new FileStream(saveFileDataDir, FileMode.Create)) {
                try {
                    bf.Serialize(fs, saveFileData);
                } catch(SerializationException e) {
                    Debug.LogError("Failed serialization of save folders info file: " + e);
                }
            }
        }

        SaveFileDisplayItem sFDI = Instantiate(saveFileDisplayPrefab, displayItemParent);
        sFDI.LoadSaveFileData(saveFileData);
    }
    #endregion

    #region Button Events
    public void SelectAikan (int uiID, int aikanID) {
        aikansSelection[prevAikanUIID].GetChild(0).GetComponent<Image>().color = defaultAikanColor;
        aikansSelection[uiID].GetChild(0).GetComponent<Image>().color = selectedAikanColor;
        selectedAikanID = aikanID;
        prevAikanUIID = uiID;
    }

    public void CreateWorld () {
        string worldName = worldNameInputField.text;
        string newSaveFolderPath = Path.Combine(saveFileDirectory, CreateFolderNameFromWorldName(worldName));

        Directory.CreateDirectory(newSaveFolderPath);
        string saveInfoFilePath = Path.Combine(newSaveFolderPath, infoFile);

        SaveFileData saveFileData = new SaveFileData() {
            aikonId = selectedAikanID,
            currentChaptre = 0,
            folderName = new DirectoryInfo(newSaveFolderPath).Name,
            fullName = worldName,
            goldenShellsCollected = 0,
            goldenShellsTotal = 0,
            shellsCollected = 0,
            shellsTotal = 0
        };
        using(FileStream fs = new FileStream(saveInfoFilePath, FileMode.Create)) {
            try {
                bf.Serialize(fs, saveFileData);
            } catch(SerializationException e) {
                Debug.LogError("Failed serialization of save folders info file: " + e);
            }
        }

        LoadSaveDisplay(newSaveFolderPath);
    }

    const string ipMatch = "^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]).){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]):[0-9]+$";
    public void JoinServer () {
        string serverIP = serverIPField.text;
        if(System.Text.RegularExpressions.Regex.IsMatch(serverIP, ipMatch)) {
            string ip = serverIP.Split(':')[0];
            ushort port = ushort.Parse(serverIP.Split(':')[1]);

            WorldSaving.inst.PrepareClient();
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            NetworkAssistant.inst.StartClient(ip, port);
        } else {
            PromptConfigurator.QueuePromptText("Invalid Adress", "The ip you requested is not valid. Try verifying if the port has the correct \":\" symbol.");
        }
    }
    #endregion

    #region Utils
    public string CreateFolderNameFromWorldName (string worldName) {
        sb.Clear();
        foreach(char c in worldName) {
            if(authorizedCharsString.Contains(c)) {
                sb.Append(c);
            } else if(char.IsWhiteSpace(c)) {
                sb.Append('_');
            }
        }

        string saveFolderPath = sb.ToString();
        while(true) {
            if(!Directory.Exists(Path.Combine(saveFileDirectory,saveFolderPath))) {
                break;
            } else {
                saveFolderPath += newNames[Random.Range(0, newNames.Length)];
            }
        }

        return saveFolderPath;
    }
    #endregion
}
