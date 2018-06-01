using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Text;

public class MainMenuController : MonoBehaviour {

    // todo: setup network connection to python server using native System.Net sockets

    [SerializeField] TextMeshProUGUI pressToStartText;
    [SerializeField] AudioClip logoCrashAudio;
    [SerializeField] AudioClip mainMenuMusic;
    [SerializeField] float menuMusicVolume = 0.5f;

    SNBNetwork snbNet;

    Animator menuAnimator;
    enum MenuScreens { None, First, Main, Settings };
    MenuScreens currentScreen = MenuScreens.None;

    private void Start()
    {
        menuAnimator = GetComponent<Animator>();
        snbNet = FindObjectOfType<SNBNetwork>();
    }

    private void Update()
    {
        if (Input.anyKeyDown && currentScreen == MenuScreens.First)
        {
            GoToMainMenu();
        }
    }

    public void PlayQuickMatch()
    {
        snbNet.GetRandomMatch((bytes) => {
            // todo: include result argument
            print("Got new match: " + Encoding.UTF8.GetString(bytes));
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
        Debug.Log("Quitting...");
        Application.Quit();
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
