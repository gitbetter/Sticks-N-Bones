using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class SNBNetwork : MonoBehaviour {

    public delegate void Loading(string message);
    public delegate void LoadingDone();
    public delegate void Error(string error);

    public event Loading OnLoad;
    public event LoadingDone OnLoadDone;
    public event Error onError;

    public string serverAddress {
        get { return _serverAddress; }
        set {
            _serverAddress = value;
            ResetSocketConnection();
        }
    }

    public int port {
        get { return _port;  }
        set {
            _port = value;
            ResetSocketConnection();
        }
    }

    private string _serverAddress = "127.0.0.1";
    private int _port = 50777;
    public int connectionRetries = 3;
    public int maxBufferSize = 1048;

    public static SNBNetwork instance = null;
    private Socket sock = null;

    private void Awake() {
        if (SNBNetwork.instance == null) {
            SNBNetwork.instance = this;
        } else if (SNBNetwork.instance != this) {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    public void InitSocketConnection() {        
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        OnLoad("Connecting to server");
        TrySocketConnection();
    }

    private void TrySocketConnection() {
        sock.BeginConnect(new[] { IPAddress.Parse(_serverAddress) }, _port, new AsyncCallback(ConnectionCallback), sock);
    }


    public void ResetSocketConnection() {
        if (sock != null && sock.Connected) {
            sock.Close();
        }
        TrySocketConnection();
    }

    private void ConnectionCallback(IAsyncResult ar) {
        try {
            sock.EndConnect(ar);
            if (!sock.Connected && connectionRetries > 0) {
                --connectionRetries;
                TrySocketConnection();
            } else {
                OnLoadDone();
            }
        } catch (SocketException ex) {
            onError("An error occurred while attempting to connect");
        }     
    }

    public void GetRandomMatch(Action<JSONObject> callback) {
        OnLoad("Looking for opponent");
        SendAndReceiveAsync(Encoding.UTF8.GetBytes("match:random"), callback);
    }

    public void TerminateConnection(Action<JSONObject> callback) {
        SendAndReceiveAsync(Encoding.UTF8.GetBytes("exit"), callback);
    }

    private void SendAndReceiveAsync(Byte[] message, Action<JSONObject> callback) {
        if (sock.Connected) {
            sock.Send(message);

            Byte[] receivedData = new Byte[maxBufferSize];
            SocketAsyncEventArgs asyncEventArgs = new SocketAsyncEventArgs();
            asyncEventArgs.SetBuffer(receivedData, 0, maxBufferSize);
            asyncEventArgs.Completed += (sender, e) => {
                JSONObject response = new JSONObject(Encoding.UTF8.GetString(e.Buffer));
                print(response);
                if (response.Count <= 0) {
                    SendAndReceiveAsync(message, callback);
                } else {
                    OnLoadDone();
                    callback(response);
                }
            };

            sock.ReceiveAsync(asyncEventArgs);
        } else {
            callback(new JSONObject());
        }
    }
}
