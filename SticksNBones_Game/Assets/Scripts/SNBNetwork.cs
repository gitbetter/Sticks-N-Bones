using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class SNBNetwork : MonoBehaviour {

    public delegate void Loading(string message);
    public delegate void LoadingDone();
    public delegate void LoadingSuccess(string message);
    public delegate void Error(string error);

    public event Loading OnLoad;
    public event LoadingDone OnLoadDone;
    public event LoadingSuccess OnLoadSuccess;
    public event Error onError;

    public string serverAddress {
        get { return _serverAddress; }
        set {
            if (_serverAddress != value) {
                _serverAddress = value;
                ResetSocketConnection();
            }
        }
    }

    public int port {
        get { return _port;  }
        set {
            if (_port != value) {
                _port = value;
                ResetSocketConnection();
            }
        }
    }

    private string _serverAddress = SNBGlobal.defaultServerIP;
    private int _port = SNBGlobal.defaultServerPort;
    private Byte[] latestData = new Byte[SNBGlobal.maxBufferSize];
    private Socket sock = null;
    private Dictionary<string, Queue<Action<JSONObject>>> callbackQueue = new Dictionary<string, Queue<Action<JSONObject>>>();

    public int connectionRetries = 10;
    public int maxBufferSize = SNBGlobal.maxBufferSize;

    public static SNBNetwork instance = null;

    private void Awake() {
        ResetValues();
        if (SNBNetwork.instance == null) {
            SNBNetwork.instance = this;
        } else if (SNBNetwork.instance != this) {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    public void InitSocketConnection() {        
        OnLoad("Connecting to server");
        TrySocketConnection();
    }

    private void TrySocketConnection() {
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        sock.BeginConnect(IPAddress.Parse(_serverAddress), _port, new AsyncCallback(ConnectionCallback), sock);
    }


    public void ResetSocketConnection() {
        if (sock != null) sock.Close();
        TrySocketConnection();
    }

    private void ConnectionCallback(IAsyncResult ar) {
        try {
            sock.EndConnect(ar);
            OnLoadSuccess("Connected to server!");
            sock.BeginReceive(latestData, 0, latestData.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
        } catch (SocketException ex) {
            if (connectionRetries > 0) {
                --connectionRetries;
                ResetSocketConnection();
            } else {
                onError("An error occurred while attempting to connect");
            }
        }
    }

    private void ReceiveCallback(IAsyncResult AR) {
        int received = sock.EndReceive(AR);
        if (received <= 0) return;

        byte[] data = new byte[received];
        Buffer.BlockCopy(latestData, 0, data, 0, received);

        JSONObject response = new JSONObject(Encoding.UTF8.GetString(data));
        if (response.Count > 0) {
            string responseRequest;
            response.GetField(out responseRequest, "request", null);

            switch(responseRequest) {
                case "match:random":
                    OnLoadSuccess("Found an opponent");
                    CallAllCallbacks(responseRequest, response);
                    break;
                default:
                    break;
            }
        }

        sock.BeginReceive(latestData, 0, latestData.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
    }

    public void GetRandomMatch(Action<JSONObject> callback) {
        if (sock.Connected) {
            OnLoad("Looking for opponent");
            SendRequest("match:random", callback);
        }
    }

    public void TerminateConnection(Action<JSONObject> callback) {
        SendRequest("exit", callback);
    }

    private void SendRequest(string request, Action<JSONObject> callback) {
        if (sock.Connected) {
            byte[] data = Encoding.UTF8.GetBytes(request);
            sock.Send(data);
            if (request == "exit") {
                sock.Close();
                callback(new JSONObject());
            } else {
                AddCallbackToQueue(request, callback);
            }
        } else {
            callback(new JSONObject());
        }
    }

    private void AddCallbackToQueue(string key, Action<JSONObject> callback) {
        if (!callbackQueue.ContainsKey(key)) {
            callbackQueue.Add(key, new Queue<Action<JSONObject>>());
        }
        callbackQueue[key].Enqueue(callback);
    }

    private List<Action<JSONObject>> TakeCallbacks(string key) {
        List<Action<JSONObject>> callbacks = new List<Action<JSONObject>>();
        if (callbackQueue.ContainsKey(key)) {
            while (callbackQueue[key].Count > 0) {
                callbacks.Add(callbackQueue[key].Dequeue());
            }
        }
        return callbacks;
    }

    private void CallAllCallbacks(string key, JSONObject response) {
        List<Action<JSONObject>> userCallbacks = TakeCallbacks(key);
        foreach (Action<JSONObject> cb in userCallbacks) {
            cb(response);
        }
    }

    private void ResetValues() {
        _serverAddress = SNBGlobal.defaultServerIP;
        _port = SNBGlobal.defaultServerPort;
        connectionRetries = 20;
        maxBufferSize = SNBGlobal.maxBufferSize;
        sock = null;
        latestData = new byte[SNBGlobal.maxBufferSize];
        callbackQueue = new Dictionary<string, Queue<Action<JSONObject>>>();
    }
}
