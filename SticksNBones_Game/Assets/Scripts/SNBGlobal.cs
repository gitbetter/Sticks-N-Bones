using System.Collections;
using System.Collections.Generic;
using System;
using System.Timers;

using UnityEngine;

public enum CharacterType { Classico, Ranger, None };
public enum PlayerStatus { NotReady, Ready, Dead /* etc. */ };
public enum PlayerRole { Local, Opponent, Sandbag, Bot };
public enum PlayerDirection { Left, Right };
public enum BasicMove { AirKick, AirPunch, Move, MoveBack,
                       MovingJump, StaticJump, Punch,
                       Kick };
public enum ComboType { Dash, DashBack };

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

    public static SNBPlayer thisPlayer = new SNBPlayer();

    public static string GetRandomUsername() {
        string adj = usernameAdjectives[rnd.Next(usernameAdjectives.Length)];
        string nn = usernameNouns[rnd.Next(usernameNouns.Length)];
        int num = rnd.Next(999);
        return adj + nn + num;
    }
}

public class SNBPlayer {
    public string username = SNBGlobal.GetRandomUsername();
    public CharacterType character = CharacterType.None;
    public PlayerStatus status = PlayerStatus.NotReady;
    public SNBPlayerState state = new SNBPlayerState();

    public void ResetState() {
        character = CharacterType.None;
        status = PlayerStatus.NotReady;
        state = new SNBPlayerState();
    }

    public void ExecuteMove(BasicMove move) {
        if (!state.inCombo) state.StartCombo();
        state.AddToCombo(move);
    }
}

public class SNBPlayerState {
    public delegate void ComboEvent(ComboType combo);
    public event ComboEvent OnComboEvent;

    public bool grounded = true;
    public bool dashing = false;
    public bool skipping = false;
    public bool blocking = false;
    public float lastHorizontal = 0f;
    public PlayerDirection facing = PlayerDirection.Right;

    private Timer comboTimer = new Timer();
    public double elapsedComboTime = 0;
    public List<MoveInfo> currentCombo = new List<MoveInfo>();

    public bool inCombo { get { return currentCombo.Count > 0; } }
    public bool idle { get { return !skipping && !dashing;  } }

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
    }

    public string ToJson() {
        return "{\"dashing\": " + dashing + ", " +
                "\"skipping\": " + skipping + "," +
                "\"blocking\": " + blocking + "," +
                "\"lastHorizontalThrow\": " + lastHorizontal + "}";
    }

    public static SNBPlayerState FromJson() {
        // todo
        return new SNBPlayerState();
    }
}