using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class MatchHandler : MonoBehaviour {

    public delegate void OpponentDisconnect();
    public delegate void OpponentConnect();
    public delegate void OpponentReady();
    public delegate void MatchTransition();

    public event OpponentDisconnect OnOpponentDisconnect;
    public event OpponentConnect OnOpponentConnect;
    public event OpponentReady OnOpponentReady;
    public event MatchTransition OnMatchTransition;

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
    [HideInInspector] public bool isServer = false;
    [HideInInspector] public SNBPlayer opponent = new SNBPlayer();

    private enum ConnectionState { Disconnected, Connected };
    private ConnectionState status = ConnectionState.Disconnected;

    private int hostId = 0, connectionId = 0;
    private byte channelId = 0;
    private string _opponentIp = null;
    private int _opponentPort = -1;

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
                HandleConnectEvent(outHostId, outConnectionId, error);
                break;
            case NetworkEventType.DataEvent:
                HandleDataEvent(outHostId, outConnectionId, buffer, error);
                break;
            case NetworkEventType.DisconnectEvent:
                HandleDisconnectEvent(outHostId, outConnectionId);
                break;
            default:
                break;
        }
        print(evnt);
    }

    private void HandleConnectEvent(int outHostId, int outConnectionId, byte error) {
        print("Peer Connected");
        if ((NetworkError)error == NetworkError.Ok) {
            status = ConnectionState.Connected;
            if (connectionId == 0) connectionId = outConnectionId;
            SendPlayerDataToOpponent();
            OnOpponentConnect();
        } else {
            print("Error connecting to peer");
            // todo: error connecting to peer
        }
    }

    private void HandleDataEvent(int outHostId, int outConnectionId, byte[] data, byte error) {
        print("Data Received");
        if ((NetworkError)error == NetworkError.Ok) {
            string messageType;
            JSONObject dataObj = new JSONObject(Encoding.UTF8.GetString(data));

            dataObj.GetField(out messageType, "messageType", null);

            JSONObject result = null;
            dataObj.GetField("result", (r) => {
                result = r;
            });

            switch (messageType) {
                case "matchup":
                    string matchupStatus;
                    result.GetField(out matchupStatus, "matchupStatus", null);

                    if (matchupStatus == "ready") {
                        int opponentCharacter;
                        result.GetField(out opponentCharacter, "playerCharacter", -1);
                        opponent.character = (CharacterType)opponentCharacter;
                        OnOpponentReady();
                    }
                    break;
                case "info":
                    string username;
                    result.GetField(out username, "username", null);
                    opponent.username = username;
                    break;
                default:
                    break;
            }
        } else {
            print("Error receiving data");
            // todo: error receiving peer data
        }
    }

    private void HandleDisconnectEvent(int outHostId, int outConnectionId) {
        print("Disconnected");
        if (outHostId == hostId &&
            outConnectionId == connectionId) {
            status = ConnectionState.Disconnected;
            OnOpponentDisconnect();
        }
    }

    public void PlayerReady() {
        SNBGlobal.thisPlayer.state = PlayerState.Ready;
        if (status == ConnectionState.Connected) {
            byte error;
            byte[] message = Encoding.UTF8.GetBytes("{\"messageType\": \"matchup\", \"result\": {\"matchupStatus\": \"ready\", \"playerCharacter\": " + SNBGlobal.thisPlayer.character + "}}");
            NetworkTransport.Send(hostId, connectionId, channelId, message, message.Length, out error);

            if (opponent.state == PlayerState.Ready) {
                OnMatchTransition();
            }

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

    public void SendPlayerDataToOpponent() {
        byte error;
        byte[] message = Encoding.UTF8.GetBytes("{'messageType': 'info', 'result': {'username': '" + SNBGlobal.thisPlayer.username + "'}}");
        NetworkTransport.Send(hostId, connectionId, channelId, message, message.Length, out error);

        if ((NetworkError)error != NetworkError.Ok) {
            print("Error sending message: " + message);
        }
    }

    private void ResetValues() {
        hostId = connectionId = channelId = 0;
        isServer = false;
        _opponentIp = null;
        _opponentPort = -1;
        status = ConnectionState.Disconnected;
    }
}
