using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class SNBNetwork : MonoBehaviour {

    [SerializeField] string serverAddress = "127.0.0.1";
    [SerializeField] int port = 50777;
    [SerializeField] int connectionRetries = 3;
    [SerializeField] int maxBufferSize = 1048;

    Socket sock;

    private void Start() {
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        TryConnection();
    }

    private void TryConnection() {
        sock.BeginConnect(new[] { IPAddress.Parse(serverAddress) }, port, new AsyncCallback(ConnectionCallback), sock);
    }

    private void ConnectionCallback(IAsyncResult ar) {
        if (!sock.Connected && connectionRetries > 0) {
            --connectionRetries;
            TryConnection();
        } else {
                // todo: could not connect to server error
        }          
    }

    public void GetRandomMatch(Action<Byte[]> callback) {
        sock.Send(Encoding.UTF8.GetBytes("match:random"));

        Byte[] receivedData = new Byte[maxBufferSize];
        SocketAsyncEventArgs asyncEventArgs = new SocketAsyncEventArgs();
        asyncEventArgs.SetBuffer(receivedData, 0, maxBufferSize);
        asyncEventArgs.Completed += (sender, e) => {
            print("Bytes Received: " + e.BytesTransferred);
            callback(e.Buffer);
        };

        sock.ReceiveAsync(asyncEventArgs);
    }
}
