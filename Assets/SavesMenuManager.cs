using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using UnityEngine.UI;
using TMPro;

public class SavesMenuManager : MonoBehaviour {

    public Transform displayItemParent;
    public SaveFileDisplayItem saveFileDisplayPrefab;
    public Sprite[] aikansSprites;

    public TMP_InputField worldNameInputField;
    public Transform[] aikansSelection;
    public Color selectedAikanColor;
    public Color defaultAikanColor;

    public static SavesMenuManager inst;

    public const string savesFolder = "saves";
    public const string infoFile = "worldInfo.wdat";

    public int selectedAikanID = 0;
    public int prevAikanUIID = 0;

    StringBuilder sb;
    char s; // Separator char
    string datapath;
    string saveFileDirectory;

    private void Awake () {
        inst = this;

        s = Path.DirectorySeparatorChar;
        datapath = Application.persistentDataPath;
        sb = new StringBuilder();

        saveFileDirectory = datapath + s + savesFolder;

        LoadAllSaves();
    }

    void LoadAllSaves () {
        string[] allSavesDir = Directory.GetDirectories(saveFileDirectory);
        List<DirectoryInfo> allSaveDirInfo = new List<DirectoryInfo>();
        foreach(string dir in allSavesDir) {
            allSaveDirInfo.Add(new DirectoryInfo(dir));
        }
        allSaveDirInfo.OrderBy(f => f.LastAccessTimeUtc);

        BinaryFormatter bf = new BinaryFormatter();

        for(int i = 0; i < allSavesDir.Length; i++) {
            string saveFileDataDir = Path.Combine(allSavesDir[i], infoFile);
            SaveFileData saveFileData = null;

            if(File.Exists(saveFileDataDir)) {
                FileStream fs = new FileStream(saveFileDataDir, FileMode.Open);
                try {
                    saveFileData = (SaveFileData)bf.Deserialize(fs);
                } catch (SerializationException e) {
                    Debug.LogError("Failed deserialization of save folders info file: " + e);
                }
            } else {
                saveFileData = new SaveFileData() {
                    aikonId = 0,
                    currentChaptre = 0,
                    fileName = new DirectoryInfo(saveFileDataDir).Name,
                    fullName = "New Save File",
                    goldenShellsCollected = 0,
                    goldenShellsTotal = 0,
                    shellsCollected = 0,
                    shellsTotal = 0
                };
                FileStream fs = new FileStream(saveFileDataDir, FileMode.Create);
                try {
                    bf.Serialize(fs, saveFileData);
                } catch(SerializationException e) {
                    Debug.LogError("Failed serialization of save folders info file: " + e);
                }
            }

            SaveFileDisplayItem sFDI = Instantiate(saveFileDisplayPrefab, displayItemParent);
            sFDI.LoadSaveFileData(saveFileData);
        }
    }

    public void SelectAikan (int uiID, int aikanID) {
        aikansSelection[prevAikanUIID].GetChild(0).GetComponent<Image>().color = defaultAikanColor;
        aikansSelection[uiID].GetChild(0).GetComponent<Image>().color = selectedAikanColor;
        selectedAikanID = aikanID;
        prevAikanUIID = uiID;
    }

    public void CreateWorld () {

    }
}
