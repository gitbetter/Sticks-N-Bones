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
                TryPeerConnection();
            }
        }
    }
    public int opponentPort {
        get { return _opponentPort; }
        set {
            _opponentPort = value;
            if (_opponentIp != null) {
                TryPeerConnection();
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

    private int hostId, connectionId;
    private byte channelId;
    private string _opponentIp = null;
    private int _opponentPort = -1;
    private CharacterType _selectedCharacter;

    private enum ConnectionState { Disconnected, Connected };
    ConnectionState status = ConnectionState.Disconnected;

    private void Awake() {
        DontDestroyOnLoad(gameObject);
        ResetValues();
        InitializeHost();
    }

    private void Update() {
        PollNetworkPeer();
    }

    private void InitializeHost() {
        NetworkTransport.Shutdown();

        NetworkTransport.Init();

        ConnectionConfig config = new ConnectionConfig();

        channelId = config.AddChannel(QosType.Reliable);

        HostTopology topology = new HostTopology(config, 10);

        // Reminder: this overload chooses a random port
        hostId = NetworkTransport.AddHost(topology);
    }

    private void TryPeerConnection() {
        byte error;
        // todo: connect should only be done on one side, based on who acts as the 'server'
        connectionId = NetworkTransport.Connect(hostId, _opponentIp, _opponentPort, 0, out error);
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
            default:
                print("Something else happened");
                break;
        }
        print(evnt);
    }

    private void HandleConnectEvent(int outHostId, int outConnectionId, byte error) {
        if (outHostId == hostId && outConnectionId == connectionId &&
            (NetworkError)error == NetworkError.Ok) {
            status = ConnectionState.Connected;
        } else {
            print("Error connecting to peer");
            // todo: error connecting to peer
        }
    }

    private void HandleDataEvent(int outHostId, int outConnectionId, byte[] data, byte error) {
        if (outHostId == hostId && outConnectionId == connectionId &&
            (NetworkError)error == NetworkError.Ok) {
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

    private void ResetValues() {
        hostId = connectionId = channelId = 0;
        _opponentIp = null;
        _opponentPort = -1;
        _selectedCharacter = CharacterType.None;
        status = ConnectionState.Disconnected;
    }
}
