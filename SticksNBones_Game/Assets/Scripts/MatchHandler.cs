using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class MatchHandler : MonoBehaviour {

    public string opponentIp {
        get { return _opponentIp; }
        set {
            _opponentIp = value;
            if (_opponentPort != -1) {
                SetupMatchConnection();
            }
        }
    }
    public int opponentPort {
        get { return _opponentPort; }
        set {
            _opponentPort = value;
            if (_opponentIp != null) {
                SetupMatchConnection();
            }
        }
    }
    public CharacterType selectedCharacter {
        get { return _selectedCharacter; }
        set {
            if (value != _selectedCharacter) {
                _selectedCharacter = value;
                if (_selectedCharacter != CharacterType.None) PlayerReady();
            }
        }
    }
    [HideInInspector] public bool isServer = false;

    private int hostId = 0, connectionId = 0;
    private byte channelId = 0;
    private string _opponentIp = null;
    private int _opponentPort = -1;
    private CharacterType _selectedCharacter = CharacterType.None;

    private enum ConnectionState { Disconnected, Connected };
    private ConnectionState status = ConnectionState.Disconnected;

    private void Awake() {
        ResetValues();
        DontDestroyOnLoad(gameObject);
    }

    private void Update() {
        PollNetworkPeer();
    }

    private void SetupMatchConnection() {
        InitializeHost();
        ConnectToPeer();
    }

    private void InitializeHost() {
        NetworkTransport.Shutdown();

        NetworkTransport.Init();

        ConnectionConfig config = new ConnectionConfig();

        channelId = config.AddChannel(QosType.Reliable);

        HostTopology topology = new HostTopology(config, 10);

        hostId = isServer ? NetworkTransport.AddHost(topology, SNBGlobal.defaultMatchPort) : NetworkTransport.AddHost(topology);
    }

    private void ConnectToPeer() {
        byte error;
        if (!isServer) {
            connectionId = NetworkTransport.Connect(hostId, _opponentIp, SNBGlobal.defaultMatchPort, 0, out error);
            if ((NetworkError)error != NetworkError.Ok) {
                print("Error connecting to peer: error");
            }
        }
    }

    private void PollNetworkPeer() {
        int outHostId, outConnectionId, outChannelId, receiveSize;
        byte[] buffer = new byte[SNBGlobal.maxBufferSize];
        byte error;
        NetworkEventType evnt = NetworkTransport.Receive(out outHostId, out outConnectionId, out outChannelId, buffer, SNBGlobal.maxBufferSize, out receiveSize, out error);
        switch(evnt) {
            case NetworkEventType.ConnectEvent:
                print("Peer Connected");
                HandleConnectEvent(outHostId, outConnectionId, error);
                break;
            case NetworkEventType.DataEvent:
                print("Data Received");
                HandleDataEvent(outHostId, outConnectionId, buffer, error);
                break;
            case NetworkEventType.DisconnectEvent:
                print("Disconnected");
                HandleDisconnectEvent(outHostId, outConnectionId);
                break;
            case NetworkEventType.Nothing:
                print("Nothing happened");
                break;
            default:
                break;
        }
        print(evnt);
    }

    private void HandleConnectEvent(int outHostId, int outConnectionId, byte error) {
        if ((NetworkError)error == NetworkError.Ok) {
            status = ConnectionState.Connected;
            if (connectionId == 0) connectionId = outConnectionId;
        } else {
            print("Error connecting to peer");
            // todo: error connecting to peer
        }
    }

    private void HandleDataEvent(int outHostId, int outConnectionId, byte[] data, byte error) {
        if ((NetworkError)error == NetworkError.Ok) {
            print(data);
        } else {
            print("Error receiving data");
            // todo: error receiving peer data
        }
    }

    private void HandleDisconnectEvent(int outHostId, int outConnectionId) {
        if (outHostId == hostId &&
            outConnectionId == connectionId) {
            status = ConnectionState.Disconnected;
        }
    }

    public void PlayerReady() {
        if (status == ConnectionState.Connected) {
            byte error;
            byte[] message = Encoding.UTF8.GetBytes("matchup:ready");
            NetworkTransport.Send(hostId, connectionId, channelId, message, message.Length, out error);

            if ((NetworkError)error != NetworkError.Ok) {
                print("Error sending message: " + message);
            }
        }
    }

    public void LeaveMatch() {
        if (status == ConnectionState.Connected) {
            byte error;
            NetworkTransport.Disconnect(hostId, connectionId, out error);

            if ((NetworkError)error != NetworkError.Ok) {
                print("Error disconnecting from peer. Will disconnect by timeout.");
            }
        }
    }

    private void ResetValues() {
        hostId = connectionId = channelId = 0;
        isServer = false;
        _opponentIp = null;
        _opponentPort = -1;
        _selectedCharacter = CharacterType.None;
        status = ConnectionState.Disconnected;
    }
}
