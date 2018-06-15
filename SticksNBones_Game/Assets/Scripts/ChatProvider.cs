using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ChatProvider : MonoBehaviour {

    [SerializeField] Text chatMessagePrefab;

    private GameObject chatContent;
    private InputField chatInput;
    private ScrollRect chatScroll;
    private CanvasGroup container;

    private bool active = false;
    private bool canAutoScroll = true;
    private bool isListening = false;
    private Queue<Action> mainThreadEvents = new Queue<Action>();

    private void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    private void Start () {
        container = GetComponentInChildren<CanvasGroup>();
        chatContent = GetComponentInChildren<VerticalLayoutGroup>().gameObject;
        chatInput = GetComponentInChildren<InputField>();
        chatScroll = GetComponentInChildren<ScrollRect>();
    }

	private void Update () {
        if (SNBNetwork.instance != null && !isListening) {
            SNBNetwork.instance.OnChatMessage += HandleIncomingMessage;
            isListening = true;
        }

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C)) {
            ToggleChat();
        } else if (Input.GetKeyDown(KeyCode.Return)) {
            SendChatMessage(chatInput.text);
        }

        DispatchActions();
    }

    private void DispatchActions() {
        while (mainThreadEvents.Count > 0) {
            Action action = mainThreadEvents.Dequeue();
            action();
        }
    }

    public void ToggleChat() {
        active = !active;
        container.interactable = container.blocksRaycasts = active;
        container.alpha = active ? 1.0f : 0.0f;
        if (active) ClearInput();
    }

    public void SendChatMessage(string message) {
        if (message != "") {
            AddChatMessage(SNBGlobal.thisUser.username + ": " + message);
            SNBNetwork.instance.SendChatMessage(SNBGlobal.thisUser.username, message);
            ClearInput();
        }
    }

    private void ClearInput() {
        chatInput.text = "";
        chatInput.Select();
        chatInput.ActivateInputField();
    }

    public void HandleIncomingMessage(string username, string message) {
        mainThreadEvents.Enqueue(() => {
            AddChatMessage(username + ": " + message);
        });
    }

    public void AddChatMessage(string message) {
        Text chatMessage = Instantiate(chatMessagePrefab);
        chatMessage.transform.SetParent(chatContent.transform, false);
        chatMessage.text = message;
        ScrollToBottom();
    }

    private void ScrollToBottom() {
        if (canAutoScroll) {
            Canvas.ForceUpdateCanvases();
            chatContent.GetComponent<VerticalLayoutGroup>().CalculateLayoutInputVertical();
            chatContent.GetComponent<ContentSizeFitter>().SetLayoutVertical();
            chatScroll.verticalNormalizedPosition = 0;
        }
    }
}
