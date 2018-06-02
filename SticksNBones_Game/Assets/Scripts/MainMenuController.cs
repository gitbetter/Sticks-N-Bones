using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class MainMenuController : MonoBehaviour {

    [SerializeField] TextMeshProUGUI pressToStartText;
    [SerializeField] AudioClip logoCrashAudio;
    [SerializeField] AudioClip mainMenuMusic;
    [SerializeField] float menuMusicVolume = 0.5f;

    SNBNetwork snbNet;
    Animator menuAnimator;
    enum MenuScreens { None, First, Main, Settings };
    MenuScreens currentScreen = MenuScreens.None;
    Queue<Action> mainThreadEvents = new Queue<Action>();

    private void Start()
    {
        menuAnimator = GetComponent<Animator>();
        snbNet = FindObjectOfType<SNBNetwork>();        
    }

    private void Update() {
        NavigateMenu();
        DispatchActions();
    }

    private void DispatchActions() {
        while (mainThreadEvents.Count > 0) {
            Action action = mainThreadEvents.Dequeue();
            action();
        }
    }

    private void NavigateMenu() {
        if (Input.anyKeyDown && currentScreen == MenuScreens.First) {
            GoToMainMenu();
        }
    }

    public void PlayQuickMatch()
    {
        snbNet.GetRandomMatch((bytes) => {
            mainThreadEvents.Enqueue(() => {
                JSONObject response = new JSONObject(Encoding.UTF8.GetString(bytes));
                if (response.Count > 0) {
                    // Todo: continue processing. Transition "Looking for opponent" component
                }
            });        
        });
    }

    public void ShowLeaderboards()
    {
        // todo
    }

    public void ShowSettings()
    {
        // todo
    }

    public void QuitGame()
    {
        snbNet.TerminateConnection((bytes) => {
            mainThreadEvents.Enqueue(() => {
                Debug.Log("Quitting...");
                Application.Quit();
            });
        });
    }

    private void PlayLogoCrash()
    {
        GetComponent<AudioSource>().PlayOneShot(logoCrashAudio);
    }

    private void PlayMenuMusic()
    {
        GetComponent<AudioSource>().PlayOneShot(mainMenuMusic, menuMusicVolume);
    }

    private void LoadStart()
    {
        currentScreen = MenuScreens.First;
        menuAnimator.SetBool("FirstLoad", true);
    }

    private void GoToMainMenu()
    {
        menuAnimator.SetBool("MainLoad", true);
    }
}
