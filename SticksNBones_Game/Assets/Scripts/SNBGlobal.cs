using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterType { None, Classico, Ranger };

public static class SNBGlobal : object {
    public static string defaultServerIP = "127.0.0.1";
    public static int defaultServerPort = 50777;
    public static int maxBufferSize = 2048;
}
