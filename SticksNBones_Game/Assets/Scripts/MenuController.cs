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

    [Header("General")]
    [SerializeField] TextMeshProUGUI pressToStartText;
    [SerializeField] AudioClip logoCrashAudio;
    [SerializeField] AudioClip mainMenuMusic;
    [SerializeField] float menuMusicVolume = 0.5f;

    [Header("Character Select")]
    [SerializeField] Sprite[] characterSprites;

    [Header("Settings Menu")]
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] Dropdown qualityDropdown;
    [SerializeField] Dropdown resolutionsDropdown;

    [Header("Network")]
    [SerializeField] SNBNetwork networkController;

    private enum MenuScreens { None, First, Main, Settings };
    private enum CharacterType { Classico, Ranger };

    MenuScreens currentScreen = MenuScreens.None;
    Queue<Action> mainThreadEvents = new Queue<Action>();
    CharacterType currentSprite = CharacterType.Classico;
    Animator menuAnimator;
    Resolution[] resolutions;

    private void Start()
    {
        menuAnimator = GetComponent<Animator>();
        if (SNBNetwork.instance == null)
            Instantiate(networkController);   
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
        SNBNetwork.instance.GetRandomMatch((bytes) => {
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
        SNBNetwork.instance.TerminateConnection((bytes) => {
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

    public void SetupCharacterSelection() {
        PreselectCharacter();
    }

    private void PreselectCharacter() {
        GameObject.FindGameObjectsWithTag("CharacterSelectionContainer")[(int)currentSprite].GetComponentInChildren<Button>().Select();
    }

    public void SetupMainMenu() {
        PreselectMenuOption();
    }

    private void PreselectMenuOption() {
        GameObject.FindGameObjectsWithTag("MainMenuOption")[0].GetComponent<Button>().Select();
    }

    public void SetupSettingsMenu() {
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        SetResolutionOptions();
        PreselectSettingsOption();
    }

    private void PreselectSettingsOption() {
        GameObject[] settingsOptions = GameObject.FindGameObjectsWithTag("SettingsMenuOption");
        foreach (GameObject opt in settingsOptions) {
            Slider volumeSlider = opt.GetComponent<Slider>();
            if (volumeSlider) {
                volumeSlider.Select();
                break;
            }
        }
    }

    private void SetResolutionOptions() {
        resolutions = Screen.resolutions;

        resolutionsDropdown.ClearOptions();

        List<string> resolutionOptions = new List<string>();

        int currentResolutionIdx = 0;
        for (int i = 0; i < resolutions.Length; i++) {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            resolutionOptions.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height) {
                currentResolutionIdx = i;
            }
        }

        resolutionsDropdown.AddOptions(resolutionOptions);
        resolutionsDropdown.value = currentResolutionIdx;
        resolutionsDropdown.RefreshShownValue();
    }

    private void ChangeCharacter() {
        GameObject.FindGameObjectWithTag("CharacterName").GetComponent<TextMeshProUGUI>().text = currentSprite.ToString();
        GameObject.FindGameObjectWithTag("CharacterSelectPreview").GetComponent<Image>().sprite = characterSprites[(int)currentSprite];
    }

    public void SetVolume(float volume) {
        audioMixer.SetFloat("masterVolume", volume);
    }

    public void SetQuality(int qualityIndex) {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetResolution(int resolutionIndex) {
        Screen.SetResolution(resolutions[resolutionIndex].width, resolutions[resolutionIndex].height, Screen.fullScreen);
    }

    public void SetNetworkIP(string ip) {
        // todo: check for valid ip   
        if (ip != "") {
            networkController.serverAddress = ip;
        }
    }

    public void SetNetworkPort(string port) {
        // todo: check for valid port
        if (port != "") {
            networkController.port = Int32.Parse(port);
        }
    }
}
