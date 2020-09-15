using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Text;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public class ListedServerDisplayItem : MonoBehaviour {
    public TextMeshProUGUI title;
    public TextMeshProUGUI titleShadow;
    public TextMeshProUGUI info;

    public ListedServerData listedServerData;

    int port = -1;
    ServerHttpJsonData serverData;

    StringBuilder sharedSb;
    private StringBuilder GetStringBuilder () {
        if(sharedSb == null) {
            sharedSb = new StringBuilder();
        } else {
            sharedSb.Clear();
        }
        return sharedSb;
    }

    public void LoadListedServerData (ListedServerData listedServerData) {

        if(listedServerData == null) {
            Debug.Log("Failed to load listed server data");
            Destroy(gameObject);
        }

        this.listedServerData = listedServerData;

        title.SetText(listedServerData.fullName);
        titleShadow.SetText(listedServerData.fullName);

        Refresh();
    }

    public void PlayListedServer () {
        if(port == -1) {
            PromptConfigurator.QueuePromptText("Server Connection Failed", "No connection has been made to the http server yet. " +
                "A connection has to be made to get the port of the multiplayer service. Wait and try again later.");
            return;
        }

        WorldSaving.inst.PrepareClient();
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
        NetworkAssistant.inst.StartClient(listedServerData.ip, (ushort)port);
    }

    public void Delete () {
        PromptConfigurator.QueuePromptText("Are you sure?", "Deleting this listed server is permanent.", () => {
            listedServerData.Delete();
            Destroy(gameObject);
        }, null);
    }

    public void Refresh () {
        if(!requestPending) {
            requestPending = true;

            StringBuilder sb = GetStringBuilder();
            sb.Append("Attempting to connect to the server...");
            info.SetText(sb);

            StartCoroutine(GetRequest("http://" + listedServerData.ip + ":" + listedServerData.httpPort + "/info/"));
        }
    }

    static private JsonSerializerSettings jss = new JsonSerializerSettings() {
        TypeNameHandling = TypeNameHandling.Auto,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    bool requestPending = false;
    IEnumerator GetRequest (string uri) {
        using(UnityWebRequest webRequest = UnityWebRequest.Get(uri)) {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            if(webRequest.isNetworkError) {
                Debug.LogError("Error: " + webRequest.error);

                StringBuilder sb = GetStringBuilder();
                sb.Append("The listed server is either offline or currently unreachable. ");
                sb.Append("Wait few seconds before attempting to reach it again.");
                info.SetText(sb);
            } else {
                Debug.Log("Received: " + webRequest.downloadHandler.text);
                JsonSerializerSettings jss = new JsonSerializerSettings();

                serverData = JsonConvert.DeserializeObject<ServerHttpJsonData>(webRequest.downloadHandler.text, jss);

                port = serverData.mlapiPort;

                StringBuilder sb = GetStringBuilder();
                sb.Append("Players online: <color=#B6D53C>");
                sb.Append(serverData.playersConnected);
                sb.Append("</color>\n");
                sb.Append(serverData.message);
                info.SetText(sb);
            }
        }
        requestPending = false;
    }
}
