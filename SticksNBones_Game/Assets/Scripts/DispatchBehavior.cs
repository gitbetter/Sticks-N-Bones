using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DispatchBehavior : MonoBehaviour {
    protected Queue<Action> mainThreadEvents = new Queue<Action>();

    protected void DispatchActions() {
        while (mainThreadEvents.Count > 0) {
            Action action = mainThreadEvents.Dequeue();
            action();
        }
    }
}
