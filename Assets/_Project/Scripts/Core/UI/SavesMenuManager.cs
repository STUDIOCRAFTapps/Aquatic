using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
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

    [Header("Prebas")]
    public SaveFileDisplayItem saveFileDisplayPrefab;
    public ListedServerDisplayItem listedServerDisplayPrefab;

    [Header("References")]
    public Transform displayItemParent;
    public TMP_InputField worldNameField;
    public Transform[] aikansSelection;
    public TMP_InputField directIPField;
    public TMP_InputField listedServerNameField;
    public TMP_InputField listedServerIPField;
    public GameObject optionMenuDisplay;
    public GameObject saveMenuDisplay;
    public SaveOptionMenuManager saveOptionMenuManager;

    [Header("Assets")]
    public Sprite[] aikansSprites;
    public Color selectedAikanColor;
    public Color defaultAikanColor;

    public static SavesMenuManager inst;

    public const string savesFolder = "saves";
    public const string backupsFolder = "backups";
    public const string listedServersFolder = "listedServers";
    

    [HideInInspector] public int selectedAikanID = 0;
    [HideInInspector] public int prevAikanUIID = 0;
    
    StringBuilder sb;
    char s; // Separator char
    string datapath;
    public string allSavesPath;
    public string allBackupsPath;
    public string allListedServersPath;

    List<SaveFileDisplayItem> saveFileDisplays;

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

        datapath = Application.persistentDataPath;
        s = Path.DirectorySeparatorChar;
        sb = new StringBuilder();
        saveFileDisplays = new List<SaveFileDisplayItem>();

        allSavesPath = datapath + s + savesFolder;
        allBackupsPath = datapath + s + backupsFolder;
        allListedServersPath = datapath + s + listedServersFolder;

        if(!Directory.Exists(allSavesPath)) {
            Directory.CreateDirectory(allSavesPath);
        }

        if(!Directory.Exists(allBackupsPath)) {
            Directory.CreateDirectory(allBackupsPath);
        }

        if(!Directory.Exists(allListedServersPath)) {
            Directory.CreateDirectory(allListedServersPath);
        }

        LoadAllSaveDisplay();
        LoadAllListedServerDisplay();
    }

    #region Save Display Loaders
    // Loads all save displays present in the save folder
    void LoadAllSaveDisplay () {
        string[] allSavesDir = Directory.GetDirectories(allSavesPath);
        List<DirectoryInfo> allSaveDirInfo = new List<DirectoryInfo>();
        foreach(string dir in allSavesDir) {
            allSaveDirInfo.Add(new DirectoryInfo(dir));
        }
        allSaveDirInfo.OrderBy(f => f.LastAccessTimeUtc);
        
        for(int i = 0; i < allSaveDirInfo.Count; i++) {
            LoadSaveDisplay(allSaveDirInfo[i].FullName);
        }
    }

    // Loads a save given the path
    public void LoadSaveDisplay (string anySaveFolderPath) {
        SaveFileData saveFileData = null;

        saveFileData = SaveFileData.Load(anySaveFolderPath);
        if(saveFileData != null) {
            string folderName = new DirectoryInfo(anySaveFolderPath).Name;
            if(saveFileData.folderName != folderName) {
                saveFileData.folderName = folderName;

                saveFileData.Save();
            }
        }

        SaveFileDisplayItem sFDI = Instantiate(saveFileDisplayPrefab, displayItemParent);
        sFDI.LoadSaveFileData(saveFileData);
        saveFileDisplays.Add(sFDI);
    }

    public SaveFileDisplayItem GetSaveFileDisplayFromData (SaveFileData saveFileData) {
        foreach(SaveFileDisplayItem sFDI in saveFileDisplays) {
            if(sFDI.saveFileData == saveFileData) {
                return sFDI;
            }
        }
        return null;
    }
    #endregion

    #region Listed Server Display Loaders
    // Loads all save displays present in the save folder
    void LoadAllListedServerDisplay () {
        string[] allListedServerFiles = Directory.GetFiles(allListedServersPath);
        List<FileInfo> allListedServerFileInfo = new List<FileInfo>();
        foreach(string dir in allListedServerFiles) {
            allListedServerFileInfo.Add(new FileInfo(dir));
        }
        allListedServerFileInfo.OrderBy(f => f.LastAccessTimeUtc);

        for(int i = 0; i < allListedServerFileInfo.Count; i++) {
            LoadListedServerDisplay(allListedServerFileInfo[i].FullName);
        }
    }

    // Loads a save given the path
    public void LoadListedServerDisplay (string anyListedServerPath) {
        ListedServerData listedServerData = null;

        if(File.Exists(anyListedServerPath)) {
            listedServerData = ListedServerData.Load(anyListedServerPath);
        } else {
            return;
        }

        ListedServerDisplayItem sFDI = Instantiate(listedServerDisplayPrefab, displayItemParent);
        sFDI.LoadListedServerData(listedServerData);
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
        try {
            string worldName = worldNameField.text;

            SaveFileData saveFileData = new SaveFileData() {
                fullName = worldName,
                aikanId = selectedAikanID,
                clientProtectionSalt = new byte[] {
                    (byte)UnityEngine.Random.Range(0, 255),
                    (byte)UnityEngine.Random.Range(0, 255),
                    (byte)UnityEngine.Random.Range(0, 255),
                    (byte)UnityEngine.Random.Range(0, 255)
                }

            };
            saveFileData.GenerateFolderName(allSavesPath, allBackupsPath);
            saveFileData.Save();

            LoadSaveDisplay(saveFileData.folderDirectory);
        } catch (Exception e) {
            PromptConfigurator.QueuePromptText("Unknown Error", e.Message);
        }
    }

    public void CreateListedServer () {
        try {
            if(VerifyIP(listedServerIPField.text, out string ip, out ushort port)) {
                ListedServerData listedServerData = new ListedServerData() {
                    ip = ip,
                    httpPort = port,
                    fullName = listedServerNameField.text
                };
                listedServerData.GenerateFolderName(allListedServersPath);
                listedServerData.Save();

                LoadListedServerDisplay(listedServerData.fileDirectory);
            }
        } catch(Exception e) {
            PromptConfigurator.QueuePromptText("Unknown Error", e.Message);
        }
    }
    
    public void JoinServer () {
        try {
            if(VerifyIP(directIPField.text, out string ip, out ushort port)) {
                WorldSaving.inst.PrepareClient();
                SceneManager.LoadScene("Main", LoadSceneMode.Single);
                NetworkAssistant.inst.StartClient(ip, port);
            }
        } catch(Exception e) {
            PromptConfigurator.QueuePromptText("Unknown Error", e.Message);
        }
    }

    const string ipMatch = "^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]).){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]):[0-9]+$";
    bool VerifyIP (string serverIP, out string ip, out ushort port) {
        ip = string.Empty;
        port = 0;

        if(System.Text.RegularExpressions.Regex.IsMatch(serverIP, ipMatch)) {
            ip = serverIP.Split(':')[0];
            port = ushort.Parse(serverIP.Split(':')[1]);

            return true;
        } else {
            PromptConfigurator.QueuePromptText("Invalid Adress", "The ip you requested is not valid. " +
                    "Try verifying if the port has the correct \":\" symbol, and if it is an ip4 adress");
            return false;
        }
    }
    #endregion

    #region Option Menu
    public void OpenOptionMenuFor (SaveFileData saveFileData) {
        optionMenuDisplay.SetActive(true);
        saveMenuDisplay.SetActive(false);

        saveOptionMenuManager.LoadSaveFile(saveFileData);
    }

    public void CloseOptionMenu () {
        optionMenuDisplay.SetActive(false);
        saveMenuDisplay.SetActive(true);
    }
    #endregion
}
