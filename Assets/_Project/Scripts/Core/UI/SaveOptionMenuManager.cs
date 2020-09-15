using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections.Generic;
using System.IO;

public class SaveOptionMenuManager : MonoBehaviour {
    [Header("References")]
    public TMP_InputField worldNameField;
    public Transform[] aikansSelection;
    public TMP_Text technicalInfo;
    public Toggle editModeEnabled;
    public TMP_InputField messageField;
    public TMP_InputField portField;
    public Toggle startServerOnPlay;
    public TMP_InputField backupFolder;
    public RectTransform backupContent;
    public RectTransform backupItemPrefab;

    SaveFileData refSFD;

    [HideInInspector] public int selectedAikanID = 0;
    [HideInInspector] public int prevAikanUIID = 0;

    List<RectTransform> backupItems = new List<RectTransform>(); 

    public void LoadSaveFile (SaveFileData saveFileData) {
        refSFD = saveFileData;

        worldNameField.text = refSFD.fullName;
        editModeEnabled.isOn = refSFD.editModeEnabled;

        for(int i = 0; i < aikansSelection.Length; i++) {
            aikansSelection[i].GetChild(0).GetComponent<Image>().color = SavesMenuManager.inst.defaultAikanColor;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("Version: <color=#ff64a5>");
        sb.Append(refSFD.mainVersionNumber);
        sb.Append('.');
        sb.Append(refSFD.secondVersionNumber);
        sb.Append('.');
        sb.Append(refSFD.bugfixVersionNumber);
        sb.Append("</color>\n");
        if(true) {
            sb.Append("WILL REQUIRE AN UPGRADE TO RUN\n");
        }
        sb.Append("Profiles created: <color=#ff64a5>");
        sb.Append(Directory.GetFiles(Path.Combine(refSFD.folderDirectory,"player_data")).Length);
        sb.Append("</color>\n");
        sb.Append("Entities saved: <color=#ff64a5>");
        sb.Append(Directory.GetFiles(Path.Combine(refSFD.folderDirectory, "edit", "overworld", "entity_data")).Length);
        sb.Append("</color>\n");
        sb.Append("Chunks generated: <color=#ff64a5>");
        sb.Append(Directory.GetFiles(Path.Combine(refSFD.folderDirectory, "edit", "overworld", "chunk_data")).Length);
        sb.Append("</color>\n");
        sb.Append("Mobile chunks saved: <color=#ff64a5>");
        sb.Append(Directory.GetFiles(Path.Combine(refSFD.folderDirectory, "edit", "overworld", "mobile_chunk_data")).Length);
        sb.Append("</color>\n");
        technicalInfo.SetText(sb);

        messageField.text = refSFD.welcomeMessage;
        portField.text = refSFD.httpPort.ToString();
        startServerOnPlay.isOn = refSFD.startHostOnPlay;

        backupFolder.text = refSFD.backupDirectory;
        LoadBackupItems();
    }

    public void SaveOptions () {
        refSFD.fullName = worldNameField.text;
        refSFD.editModeEnabled = editModeEnabled.isOn;
        refSFD.aikanId = selectedAikanID;

        refSFD.welcomeMessage = messageField.text;
        if(ushort.TryParse(portField.text, out ushort result)) {
            if(result <= 9999) {
                refSFD.httpPort = result;
            } else {
                PromptConfigurator.QueuePromptText("Error while saving", "Invalid port number, must be between [0-9999]");
            }
        } else {
            PromptConfigurator.QueuePromptText("Error while saving", "Invalid port number, number can\'t be parse");
        }
        refSFD.startHostOnPlay = startServerOnPlay.isOn;
        refSFD.backupDirectory = backupFolder.text;
        if(!Directory.Exists(refSFD.backupDirectory)) {
            PromptConfigurator.QueuePromptText("Warning", "Backup folder not found (is your external drive disk plugged in?)");
        }

        refSFD.Save();
    }

    #region Events
    public void SelectAikan (int uiID, int aikanID) {
        aikansSelection[prevAikanUIID].GetChild(0).GetComponent<Image>().color = SavesMenuManager.inst.defaultAikanColor;
        aikansSelection[uiID].GetChild(0).GetComponent<Image>().color = SavesMenuManager.inst.selectedAikanColor;
        selectedAikanID = aikanID;
        prevAikanUIID = uiID;
    }

    public void UpdateVersion () {
        PromptConfigurator.QueuePromptText("Execute Action?", "Do you really want to upgrade your world to run it on the newest version? You might want to do a backup first.",
            () => {
                Debug.LogError("Save file upgraded.");
            },
            null
        );
    }

    public void UpdateBackupFolder () {
        refSFD.backupDirectory = backupFolder.text;
        if(!Directory.Exists(refSFD.backupDirectory)) {
            PromptConfigurator.QueuePromptText("Warning", "Backup folder not found (is your external drive disk plugged in?)");
        }

        LoadBackupItems();
    }

    public void LoadBackupItems () {
        foreach(RectTransform backupItem in backupItems) {
            Destroy(backupItem.gameObject);
        }
        backupItems.Clear();

        if(Directory.Exists(refSFD.backupDirectory)) {
            string[] backupsPaths = Directory.GetFiles(refSFD.backupDirectory);
            foreach(string backupPath in backupsPaths) {
                FileInfo fileInfo = new FileInfo(backupPath);
                RectTransform newBackupItem = Instantiate(backupItemPrefab, backupContent);
                newBackupItem.gameObject.SetActive(true);
                newBackupItem.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text =
                    $"Created {fileInfo.CreationTime.ToString("MMMM d, yyyy")} at {fileInfo.CreationTime.ToString("HH:mm:ss")}";
                newBackupItem.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = fileInfo.Name;

                newBackupItem.GetChild(0).GetComponent<Button>().onClick.AddListener(() => {
                    refSFD.LoadBackup(backupPath);
                });

                backupItems.Add(newBackupItem);
            }
        }
    }

    public void CreateBackup () {
        refSFD.CreateBackup();
    }

    public void DuplicateSave () {
        PromptConfigurator.QueuePromptText("Execute Action?", "Do you really want to duplicate your save file? The backups won't be duplicated.",
            () => {
                refSFD.Duplicate();
            },
            null
        );
    }

    public void StartServer () {
        PromptConfigurator.QueuePromptText("Starting Server...", "Are you ready to start the server?",
            () => {
                Debug.LogError("Server started.");
            },
            null
        );
    }

    public void ClearPlayerData () {
        PromptConfigurator.QueuePromptText("Are you sure about that?", "Your position, health and inventory will be reset. This can break your game.",
            () => {
                PromptConfigurator.QueuePromptText("Last warning.", "Click continue to clear the players data and click cancel to keep them.",
                    () => {
                        refSFD.DeletePlayerData();
                    },
                    null
                );
            },
            null
        );
    }

    public void ResetSave () {
        PromptConfigurator.QueuePromptText("Are you sure about that?", "Don\'t reset a save you've played a lot on! Once it's reset, the world will reset to how it was before you played it.",
            () => {
                PromptConfigurator.QueuePromptText("Last warning.", "Click continue to reset the save and click cancel to keep it.",
                    () => {
                        refSFD.DeletePlayData();
                    },
                    null
                );
            },
            null
        );
    }

    public void DeleteSave () {
        PromptConfigurator.QueuePromptText("Are you sure about that?", "Don't delete a save you've worked hard on! You won't be able to experience this save again, " +
            "and you might not be able to recreate it once it's gone.",
            () => {
                PromptConfigurator.QueuePromptText("Last warning.", "Click continue to delete the save and click cancel to keep it.",
                    () => {
                        refSFD.Delete();
                        CloseOptionMenu(false);
                    },
                    null
                );
            },
            () => {
                PromptConfigurator.QueuePromptText("Thank you.", "You saved this save file.");
            }
        );
    }

    public void CloseOptionMenu (bool doSave) {
        if(doSave) {
            SaveOptions();
            SaveFileDisplayItem sFDI = SavesMenuManager.inst.GetSaveFileDisplayFromData(refSFD);
            if(sFDI != null) {
                sFDI.LoadSaveFileData(refSFD);
            }
        }
        SavesMenuManager.inst.CloseOptionMenu();
    }
    #endregion
}
