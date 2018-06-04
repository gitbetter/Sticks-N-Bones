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

    private string _serverAddress = "127.0.0.1";
    private int _port = 50777;
    private ManualResetEvent connectionTimeout = new ManualResetEvent(false);
    private Socket sock = null;

    public int connectionRetries = 10;
    public int maxBufferSize = SNBGlobal.maxBufferSize;

    public static SNBNetwork instance = null;

    private void Awake() {
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
        } catch (SocketException ex) {
            if (connectionRetries > 0) {
                --connectionRetries;
                ResetSocketConnection();
            } else {
                onError("An error occurred while attempting to connect");
            }
        }
    }

    public void GetRandomMatch(Action<JSONObject> callback) {
        if (sock.Connected) {
            OnLoad("Looking for opponent");
            SendAndReceiveAsync(Encoding.UTF8.GetBytes("match:random"), "Matched with opponent", callback);
        }
    }

    public void TerminateConnection(Action<JSONObject> callback) {
        SendAndReceiveAsync(Encoding.UTF8.GetBytes("exit"), null, callback);
    }

    private void SendAndReceiveAsync(Byte[] data, string successMessage, Action<JSONObject> callback) {
        if (sock.Connected) {
            sock.Send(data);

            Byte[] receivedData = new Byte[maxBufferSize];
            SocketAsyncEventArgs asyncEventArgs = new SocketAsyncEventArgs();
            asyncEventArgs.SetBuffer(receivedData, 0, maxBufferSize);
            asyncEventArgs.Completed += (sender, e) => {
                JSONObject response = new JSONObject(Encoding.UTF8.GetString(e.Buffer));
                if (response.Count <= 0) {
                    SendAndReceiveAsync(data, successMessage, callback);
                } else {
                    if (successMessage != null) {
                        OnLoadSuccess(successMessage);
                    }
                    callback(response);
                }
            };

            sock.ReceiveAsync(asyncEventArgs);
        } else {
            callback(new JSONObject());
        }
    }
}
