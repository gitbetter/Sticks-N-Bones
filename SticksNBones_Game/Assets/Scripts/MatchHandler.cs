using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
                PlayerReady();
            }
        }
    }

    private int hostId, connectionId;
    private byte channelId;
    private string _opponentIp = null;
    private int _opponentPort = -1;
    private CharacterType _selectedCharacter;

    private void Awake() {
        DontDestroyOnLoad(gameObject);
        InitializeHost();
    }

    private void Update() {
        PollNetworkPeer();
    }

    private void InitializeHost() {
        NetworkTransport.Init();

        ConnectionConfig config = new ConnectionConfig();

        channelId = config.AddChannel(QosType.Reliable);

        HostTopology topology = new HostTopology(config, 10);

        hostId = NetworkTransport.AddHost(topology, 12345);
    }

    private void TryPeerConnection() {
        byte error;
        connectionId = NetworkTransport.Connect(hostId, opponentIp, opponentPort, 0, out error);
    }

    private void PollNetworkPeer() {
        int outHostId, outConnectionId, outChannelId, receiveSize;
        byte[] buffer = new byte[SNBGlobal.maxBufferSize];
        byte error;
        NetworkEventType evnt = NetworkTransport.Receive(out outHostId, out outConnectionId, out outChannelId, buffer, SNBGlobal.maxBufferSize, out receiveSize, out error);
        switch(evnt) {
            case NetworkEventType.ConnectEvent:
                break;
            case NetworkEventType.DataEvent:
                break;
            case NetworkEventType.DisconnectEvent:
                break;
            default:
                break;
        }
    }

    public void PlayerReady() {

    }
}
