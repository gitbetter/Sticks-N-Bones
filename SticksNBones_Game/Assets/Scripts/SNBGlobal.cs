﻿using System.Collections;
using System.Collections.Generic;
using System;
using System.Timers;

using UnityEngine;

/* Type enums */
public enum CharacterType { Classico, Ranger, None };
public enum PlayerRole { Local, Opponent, Sandbag, Bot };
public enum MatchType { P2P, Training };
/* Status enums */
public enum UserStatus { NotReady, Ready, /* etc. */ };
public enum PlayerStatus { Alive, Dead };
/* Movement and attack enums */
public enum PlayerDirection { Left, Right };
public enum BasicMove { AirKick, AirPunch, Move, MoveBack,
                       MovingJump, StaticJump, Punch,
                       Kick };
public enum ComboType { Dash, DashBack };


//    ___  ___              _____      __
//    |  \/  |              |_ _|     / _|     
//    | .  . | _____   _____ | | _ __ | |_ ___
//    | |\/| |/ _ \ \ / / _ \| || '_ \|  _/ _ \ 
//    | |  | | (_) \ V /  __/| || | | | || (_) |
//    \_|  |_/\___/ \_/ \___\___/_| |_|_| \___/ 
                                         

public struct MoveInfo {
    public BasicMove move;
    public int sequenceNumber;
    public double sequenceTime;
    public string moveKey;

    public MoveInfo(BasicMove m, int seqNum, double seqTime, string mk = null) {
        move = m;
        sequenceNumber = seqNum;
        sequenceTime = seqTime;
        moveKey = mk;
    }
}

//     _____ _   _ ______ _____ _       _           _
//    /  ___| \ | || ___ \  __ \ |     | |         | |
//    \ `--.|  \| || |_/ / |  \/ | ___ | |__   __ _| |
//     `--. \ . ` || ___ \ | __| |/ _ \| '_ \ / _` | |
//    /\__/ / |\  || |_/ / |_\ \ | (_) | |_) | (_| | |
//    \____/\_| \_/\____/ \____/_|\___/|_.__/ \__,_|_|
//

public static class SNBGlobal : object {
    public static readonly string defaultServerIP = "127.0.0.1";
    public static readonly int defaultServerPort = 50777;
    public static readonly int maxBufferSize = 2048;
    public static readonly int defaultMatchPort = 60000;
    public static readonly double comboDeltaTime = 400;

    public static readonly Dictionary<ComboType, List<BasicMove>> combos = new Dictionary<ComboType, List<BasicMove>>() {
        { ComboType.Dash, new List<BasicMove>() { BasicMove.Move, BasicMove.Move } },
        { ComboType.DashBack, new List<BasicMove>() { BasicMove.MoveBack, BasicMove.MoveBack } }
    };
    
    private static readonly string[] usernameAdjectives = {"Growling", "Floating", "Mean", "Arcadian", "Friable", "Noxious", "Luminous", "Turbulent", "Nebulous", "Arc"};
    private static readonly string[] usernameNouns = {"Ghost", "Glass", "Connection", "Pump", "Hill", "Cactus", "Nation", "Flavor", "Metal", "Spring"};

    private static System.Random rnd = new System.Random();

    public static SNBUser thisUser = new SNBUser();

    public static string GetRandomUsername() {
        string adj = usernameAdjectives[rnd.Next(usernameAdjectives.Length)];
        string nn = usernameNouns[rnd.Next(usernameNouns.Length)];
        int num = rnd.Next(999);
        return adj + nn + num;
    }

    public static GameObject FindParentWithTag(GameObject child, string tag) {
        Transform t = child.transform;
        while (t.parent != null) {
            if (t.parent.tag == tag) return t.parent.gameObject;
            else t = t.parent.transform;
        }
        return null;
    }
}

//     _____ _   _ ______ _   _
//    /  ___| \ | || ___ \ | | |              
//    \ `--.|  \| || |_/ / | | |___  ___ _ __
//     `--. \ . ` || ___ \ | | / __|/ _ \ '__|
//    /\__/ / |\  || |_/ / |_| \__ \  __/ |   
//    \____/\_| \_/\____/ \___/|___/\___|_|  
//

public class SNBUser {
    public string username = SNBGlobal.GetRandomUsername();
    public CharacterType character = CharacterType.None;
    public UserStatus status = UserStatus.NotReady;
}

//     _____ _   _ ____________ _                       
//    /  ___| \ | || ___ \ ___ \ |                      
//    \ `--.|  \| || |_/ / |_/ / | __ _ _   _  ___ _ __ 
//     `--. \ . ` || ___ \  __/| |/ _` | | | |/ _ \ '__|
//    /\__/ / |\  || |_/ / |   | | (_| | |_| |  __/ |   
//    \____/\_| \_/\____/\_|   |_|\__,_|\__, |\___|_|   
//                                       __/ |          
//                                      |___/      

public class SNBPlayer {
    public CharacterType character = CharacterType.None;
    public PlayerStatus status = PlayerStatus.Alive;
    public SNBPlayerState state = new SNBPlayerState();

    public void ResetState() {
        character = CharacterType.None;
        status = PlayerStatus.Alive;
        state = new SNBPlayerState();
    }

    public void ExecuteMove(BasicMove move) {
        if (!state.inCombo) state.StartCombo();
        state.AddToCombo(move);
    }
}

//     _____ _   _ ____________ _                       _____ _        _
//    /  ___| \ | || ___ \ ___ \ |                     /  ___| |      | |      
//    \ `--.|  \| || |_/ / |_/ / | __ _ _   _  ___ _ __\ `--.| |_ __ _| |_ ___
//     `--. \ . ` || ___ \  __/| |/ _` | | | |/ _ \ '__|`--. \ __/ _` | __/ _ \
//    /\__/ / |\  || |_/ / |   | | (_| | |_| |  __/ |  /\__/ / || (_| | ||  __/
//    \____/\_| \_/\____/\_|   |_|\__,_|\__, |\___|_|  \____/ \__\__,_|\__\___|
//                                       __/ |                                 
//                                      |___/                            

public class SNBPlayerState {
    public delegate void ComboEvent(ComboType combo);
    public delegate void DirectionFlipped();
    public delegate void StateChanged();

    public event ComboEvent OnComboEvent;
    public event DirectionFlipped OnDirectionFlipped;
    public event StateChanged OnStateChanged;

    private bool _grounded = true;
    private bool _dashing = false, _skipping = false, _blocking = false,
                _crouching = false, _attacking = false;
    private float _lastHorizontal = 0;
    private float _lastVertical = 0;
    private PlayerDirection _facing = PlayerDirection.Right;
    private Timer comboTimer = new Timer();
    private double elapsedComboTime;


    public bool grounded {
        get { return _grounded; }
        set {
            if (value != _grounded) {
                _grounded = value;
                OnStateChanged();
            }
        }
    }

    public bool dashing {
        get { return _dashing; }
        set {
            if (value != _dashing) {
                _dashing = value;
                OnStateChanged();
            }
        }
    }

    public bool skipping {
        get { return _skipping; }
        set {
            if (value != _skipping) {
                _skipping = value;
                OnStateChanged();
            }
        }
    }

    public bool blocking {
        get { return _blocking; }
        set {
            if (value != _blocking) {
                _blocking = value;
                OnStateChanged();
            }
        }
    }

    public bool crouching {
        get { return _crouching; }
        set {
            if (value != _crouching) {
                _crouching = value;
                OnStateChanged();
            }
        }
    }

    public bool attacking {
        get { return _attacking; }
        set {
            if (value != _attacking) {
                _attacking = value;
                OnStateChanged();
            }
        }
    }

    public float lastHorizontal {
        get { return _lastHorizontal; }
        set {
            if (value != _lastHorizontal) {
                _lastHorizontal = value;
                OnStateChanged();
            }
        }
    }

    public float lastVertical {
        get { return _lastVertical; }
        set {
            if (value != _lastVertical) {
                _lastVertical = value;
                OnStateChanged();
            }
        }
    }

    public List<MoveInfo> currentCombo = new List<MoveInfo>();
    public bool inCombo { get { return currentCombo.Count > 0; } }
    public bool idle { get { return !_skipping && !_dashing;  } }
    public PlayerDirection facing {
        get { return _facing; }
        set {
            if (value != _facing) {
                _facing = value;
                OnStateChanged();
                if (OnDirectionFlipped != null)
                    OnDirectionFlipped();
            }
        }
    }

    public SNBPlayerState() {
        comboTimer.Elapsed += (sender, eventArgs) => ComboElapsedHandler();
        comboTimer.Enabled = false;
    }

    private void ComboElapsedHandler() {
        elapsedComboTime += comboTimer.Interval;

        if (currentCombo.Count > 0 && 
            (elapsedComboTime - currentCombo[currentCombo.Count - 1].sequenceTime) > SNBGlobal.comboDeltaTime) {
            EndCombo();
        }
    }

    public void StartCombo() {
        comboTimer.Start();
    }

    public void AddToCombo(BasicMove move) {
        MoveInfo m = new MoveInfo(move, currentCombo.Count + 1, elapsedComboTime);
        currentCombo.Add(m);
        OnStateChanged();

        if (inCombo) {
            CheckCombo(currentCombo);
        }
    }

    private void CheckCombo(List<MoveInfo> currentCombo) {
        foreach (ComboType ct in SNBGlobal.combos.Keys) {
            if (ComboSequencesMatch(SNBGlobal.combos[ct], currentCombo)) {
                OnComboEvent(ct); return;
            }
        }
    }

    private bool ComboSequencesMatch(List<BasicMove> comboSequence, List<MoveInfo> moves) {
        if (comboSequence.Count != moves.Count) return false;
        for (int i = 0; i < comboSequence.Count; i++) {
            if (comboSequence[i] != moves[i].move) return false;
        }
        return true;
    }

    public void EndCombo() {
        comboTimer.Stop();
        currentCombo.Clear();
        elapsedComboTime = 0;
        OnStateChanged();
    }

    public string ToJson() {
        return "{\"dashing\": " + _dashing + ", " +
                "\"skipping\": " + _skipping + ", " +
                "\"blocking\": " + _blocking + ", " +
                "\"crouching\": " + _crouching + ", " +
                "\"attacking\": " + _attacking + ", " +
                "\"grounded\": " + _grounded + ", " +
                "\"lastHorizontalThrow\": " + _lastHorizontal + ", " +
                "\"lastVerticalThrow\": " + _lastVertical + "}";
    }

    public static SNBPlayerState FromJson(string state) {
        // todo
        return new SNBPlayerState();
    }
}