using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Audio;

using TMPro;

public class MenuController : MonoBehaviour {

    [SerializeField] TextMeshProUGUI pressToStartText;
    [SerializeField] AudioClip logoCrashAudio;
    [SerializeField] AudioClip mainMenuMusic;
    [SerializeField] float menuMusicVolume = 0.5f;
    [SerializeField] Sprite[] characterSprites;
    [SerializeField] AudioMixer audioMixer;

    private enum MenuScreens { None, First, Main, Settings };
    private enum CharacterType { Classico, Ranger };

    MenuScreens currentScreen = MenuScreens.None;
    Queue<Action> mainThreadEvents = new Queue<Action>();
    CharacterType currentSprite = CharacterType.Classico;
    SNBNetwork snbNet;
    Animator menuAnimator;

    private void Start()
    {
        menuAnimator = GetComponent<Animator>();
        snbNet = FindObjectOfType<SNBNetwork>();        
    }

    private void Update() {
        NavigateMenu();
        DispatchActions();
        SelectedCharacterChanged();
    }

    private void NavigateMenu() {
        if (Input.anyKeyDown && currentScreen == MenuScreens.First) {
            GoToMainMenu();
        }
    }

    private void DispatchActions() {
        while (mainThreadEvents.Count > 0) {
            Action action = mainThreadEvents.Dequeue();
            action();
        }
    }

    private void SelectedCharacterChanged() {
        GameObject[] selectionContainers = GameObject.FindGameObjectsWithTag("CharacterSelectionContainer");
        for (int i = 0; i < selectionContainers.Length; i++) {
            GameObject cont = selectionContainers[i];
            if (cont.GetComponentInChildren<Button>().gameObject == EventSystem.current.currentSelectedGameObject) {
                cont.GetComponent<Image>().color = new Color(1f, 0.9896f, 0);
                if ((int)currentSprite != i) {
                    currentSprite = (CharacterType)i;
                    menuAnimator.Play("CharacterSelectionChangedAnimation", -1, 0f);
                }
            } else {
                cont.GetComponent<Image>().color = new Color(1f, 1f, 1f);
            }
        }
    }

    public void PlayQuickMatch()
    {
        GoToCharacterSelect();
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
        GoToSettings();
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

    public void PlayLogoCrash()
    {
        GetComponent<AudioSource>().PlayOneShot(logoCrashAudio);
    }

    public void PlayMenuMusic()
    {
        GetComponent<AudioSource>().PlayOneShot(mainMenuMusic, menuMusicVolume);
    }

    public void LoadStart()
    {
        currentScreen = MenuScreens.First;
        menuAnimator.SetBool("FirstLoad", true);
    }

    public void GoToMainMenu()
    {
        menuAnimator.SetBool("MainLoad", true);
    }

    public void GoToCharacterSelect() {
        menuAnimator.SetBool("CharacterSelectLoad", true);
    }

    public void CharacterSelectToMain() {
        menuAnimator.SetBool("CharacterSelectLoad", false);
    }

    public void GoToSettings() {
        menuAnimator.SetBool("SettingsLoad", true);
    }

    public void SettingsToMain() {
        menuAnimator.SetBool("SettingsLoad", false);
    }

    private void PreselectCharacter() {
        GameObject.FindGameObjectsWithTag("CharacterSelectionContainer")[(int)currentSprite].GetComponentInChildren<Button>().Select();
    }

    private void PreselectMenuOption() {
        GameObject.FindGameObjectsWithTag("MainMenuOption")[0].GetComponent<Button>().Select();
    }

    private void ChangeCharacter() {
        GameObject.FindGameObjectWithTag("CharacterName").GetComponent<TextMeshProUGUI>().text = currentSprite.ToString();
        GameObject.FindGameObjectWithTag("CharacterSelectPreview").GetComponent<Image>().sprite = characterSprites[(int)currentSprite];
    }

    public void SetVolume(float volume) {
        audioMixer.SetFloat("masterVolume", volume);
    }
}
