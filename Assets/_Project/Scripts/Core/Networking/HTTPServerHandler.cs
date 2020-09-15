using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using UnityEngine;
using Newtonsoft.Json;
using System.Net.Sockets;

public class HTTPServerHandler : MonoBehaviour {

    #region Preparing Data
    ushort mlapiPort = 7778;
    SaveFileData saveFileData;
    object httpDataLock = new object();
    byte[] httpData;

    static private JsonSerializerSettings jss = new JsonSerializerSettings() {
        TypeNameHandling = TypeNameHandling.Auto,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    void Start () {
        if(!NetworkAssistant.inst.IsServer) {
            Destroy(this);
            return;
        }
        
        serverRunning = true;
        UpdateServerData(NetworkAssistant.inst.PlayerCount);
        ThreadPool.QueueUserWorkItem((s) => {
            StartListener();
        });
    }

    private void UpdateServerData (int playerCount) {
        mlapiPort = NetworkAssistant.inst.HostPort;
        saveFileData = SaveFileData.Load(WorldSaving.inst.currentSaveFolder);
        ServerHttpJsonData serverHttpJsonData = new ServerHttpJsonData(saveFileData.welcomeMessage, playerCount, mlapiPort, saveFileData.clientProtectionSalt);

        lock(httpDataLock) {
            httpData = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(serverHttpJsonData));
        }
    }


    private void OnEnable () {
        NetworkAssistant.inst.OnNetworkDataChanged += UpdateServerData;
    }

    private void OnDisable () {
        NetworkAssistant.inst.OnNetworkDataChanged -= UpdateServerData;
    }

    private void OnDestroy () {
        serverRunning = false;
    }

    public static bool PortInUse (ushort port) {
        bool inUse = false;

        IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
        IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
        
        foreach(IPEndPoint endPoint in ipEndPoints) {
            if(endPoint.Port == port) {
                inUse = true;
                break;
            }
        }
        
        return inUse;
    }
    #endregion

    volatile bool serverRunning = false;
    public void StartListener () {
        
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://*:7777/info/");
        listener.Start();

        while(serverRunning) {
            IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
            result.AsyncWaitHandle.WaitOne();
        }

        // It will only work if I add this: System.Threading.Thread.Sleep(200);
        listener.Close();
    }

    public void ListenerCallback (IAsyncResult result) {
        HttpListener listener = (HttpListener)result.AsyncState;

        // It throws the exception at this line
        HttpListenerContext context = listener.EndGetContext(result);

        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        lock(httpDataLock) {
            response.ContentLength64 = httpData.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(httpData, 0, httpData.Length);
            output.Close();
        }
    }
}
