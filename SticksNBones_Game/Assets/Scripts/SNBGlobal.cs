using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterType { Classico, Ranger, None };

public static class SNBGlobal : object {
    public static string defaultServerIP = "127.0.0.1";
    public static int defaultServerPort = 50777;
    public static int maxBufferSize = 2048;
    public static int defaultMatchPort = 60000;
}
