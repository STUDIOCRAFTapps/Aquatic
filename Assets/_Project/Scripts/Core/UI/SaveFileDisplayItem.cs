using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;
using UnityEngine.SceneManagement;

public class SaveFileDisplayItem : MonoBehaviour {
    public TextMeshProUGUI title;
    public TextMeshProUGUI titleShadow;
    public TextMeshProUGUI info;
    public Image aikanImage;

    public SaveFileData saveFileData;

    public void LoadSaveFileData (SaveFileData saveFileData) {
        if(saveFileData == null) {
            Debug.Log("Failed to load save file data info");
            Destroy(gameObject);
        }

        this.saveFileData = saveFileData;

        title.SetText(saveFileData.fullName);
        titleShadow.SetText(saveFileData.fullName);

        aikanImage.sprite = SavesMenuManager.inst.aikansSprites[saveFileData.aikonId];

        StringBuilder sb = new StringBuilder();
        sb.Append("Chapter ");
        sb.Append(saveFileData.currentChaptre + 1);
        sb.Append('\n');
        sb.Append('\n');
        sb.Append("Cannot retrieve health information yet.");
        sb.Append('\n');
        sb.Append("Cannot retrieve location information yet.");
        sb.Append('\n');
        sb.Append('\n');
        sb.Append("Shells Collected: <color=#50EDF4>");
        sb.Append(saveFileData.shellsCollected);
        sb.Append("/");
        sb.Append(saveFileData.shellsTotal);
        sb.Append("</color>");
        sb.Append('\n');
        sb.Append("Golden Shells Collected: <color=#50EDF4>");
        sb.Append(saveFileData.goldenShellsCollected);
        sb.Append("/");
        sb.Append(saveFileData.goldenShellsTotal);
        sb.Append("</color>");

        info.SetText(sb);
    }

    public void PlaySaveFileData () {
        string ip = ((RufflesTransport.RufflesTransport)MLAPI.NetworkingManager.Singleton.NetworkConfig.NetworkTransport).ConnectAddress;
        string port = ((RufflesTransport.RufflesTransport)MLAPI.NetworkingManager.Singleton.NetworkConfig.NetworkTransport).Port.ToString();

        WorldSaving.inst.PrepareNewSave(saveFileData.folderName);
        SceneManager.LoadScene("Main", LoadSceneMode.Single);

        NetworkAssistant.inst.StartHost();
    }
}
