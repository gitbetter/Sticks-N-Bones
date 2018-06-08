using System.Collections;
using System.Collections.Generic;
using System;

public enum CharacterType { Classico, Ranger, None };
public enum PlayerStatus { NotReady, Ready, Dead /* etc. */ };
public enum PlayerRole { Local, Opponent, Sandbag, Bot }

public static class SNBGlobal : object {
    public static readonly string defaultServerIP = "127.0.0.1";
    public static readonly int defaultServerPort = 50777;
    public static readonly int maxBufferSize = 2048;
    public static readonly int defaultMatchPort = 60000;
    
    private static readonly string[] usernameAdjectives = {"Growling", "Floating", "Mean", "Arcadian", "Friable", "Noxious", "Luminous", "Turbulent", "Nebulous", "Arc"};
    private static readonly string[] usernameNouns = {"Ghost", "Glass", "Connection", "Pump", "Hill", "Cactus", "Nation", "Flavor", "Metal", "Spring"};

    private static Random rnd = new Random();

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
}

public class SNBPlayerState {
    public bool dashing = false;
    public bool skipping = false;
    public bool blocking = false;
    public float lastHorizontalThrow = 0f;
    // todo: last combo array

    public string ToJson() {
        return "{}";
    }
}