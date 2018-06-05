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
    [SerializeField] float messageTTL = 3.0f;
    [SerializeField] Sprite successSprite;
    [SerializeField] Sprite errorSprite;
    [SerializeField] Sprite loadingSprite;

    [Header("Matchmaking")]
    [SerializeField] MatchHandler currentMatch;

    private enum MenuScreens { None, First, Main, Settings, Character };

    MenuScreens currentScreen = MenuScreens.None;
    Queue<Action> mainThreadEvents = new Queue<Action>();
    CharacterType currentCharacter = CharacterType.Classico;
    Animator menuAnimator;
    Resolution[] resolutions;

    private void Awake() {
        menuAnimator = GetComponent<Animator>();
    }

    private void Update() {
        NavigateMenu();
        DispatchActions();
        SelectedCharacterChanged();
    }

    private void InitializeNetwork() {
        if (SNBNetwork.instance == null)
            Instantiate(networkController);

        networkController.OnLoad += ShowConnectionLoad;
        networkController.OnLoadDone += DismissConnectionMessage;
        networkController.OnLoadSuccess += ShowConnectionSuccess;
        networkController.onError += ShowConnectionError;

        networkController.InitSocketConnection();
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
                if ((int)currentCharacter != i) {
                    currentCharacter = (CharacterType)i;
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
        networkController.GetRandomMatch((response) => {
            mainThreadEvents.Enqueue(() => {
                if (response.Count > 0) {
                    string oppIp; int oppPort;
                    Instantiate(currentMatch);
                    // todo: also receive 'is_server' field
                    response.GetField(out oppIp, "ip", null);
                    response.GetField(out oppPort, "port", -1);
                    currentMatch.opponentIp = oppIp;
                    currentMatch.opponentPort = oppPort;
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
        networkController.TerminateConnection((bytes) => {
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
        currentScreen = MenuScreens.Main;
        menuAnimator.SetBool("MainLoad", true);
    }

    public void GoToCharacterSelect() {
        currentScreen = MenuScreens.Character;
        menuAnimator.SetBool("CharacterSelectLoad", true);
    }

    public void CharacterSelectToMain() {
        currentScreen = MenuScreens.Main;
        menuAnimator.SetBool("CharacterSelectLoad", false);
    }

    public void GoToSettings() {
        currentScreen = MenuScreens.Settings;
        menuAnimator.SetBool("SettingsLoad", true);
    }

    public void ShowConnectionLoad(string message) { 
        mainThreadEvents.Enqueue(() => {
            GameObject connectionInfo = GameObject.FindGameObjectWithTag("ConnectionInfoMessage");
            if (!IsConnectionMessageShowing()) {
                connectionInfo.GetComponent<Animator>().Play("MessagePopupAnimation", 0, 0f);
            }
            connectionInfo.GetComponentInChildren<Image>().sprite = loadingSprite;
            connectionInfo.GetComponentInChildren<LoadingSpinner>().rotating = true;
            connectionInfo.GetComponentInChildren<TextMeshProUGUI>().text = message;
        });
    }

    public void ShowConnectionSuccess(string message) {
        mainThreadEvents.Enqueue(() => {
            GameObject connectionInfo = GameObject.FindGameObjectWithTag("ConnectionInfoMessage");
            connectionInfo.GetComponent<Animator>().Play("MessageAlertAnimation", 0, 0f);
            connectionInfo.GetComponentInChildren<TextMeshProUGUI>().text = message;
            connectionInfo.GetComponentInChildren<Image>().sprite = successSprite;
            connectionInfo.GetComponentInChildren<LoadingSpinner>().rotating = false;
            Invoke("DismissConnectionMessage", messageTTL);            
        });
    }

    public void DismissConnectionMessage() {
        mainThreadEvents.Enqueue(() => {
            GameObject connectionInfo = GameObject.FindGameObjectWithTag("ConnectionInfoMessage");
            if (!IsCurrentAnimationClip(connectionInfo.GetComponent<Animator>(), "MessagePopdownAnimation")) {
                connectionInfo.GetComponent<Animator>().Play("MessagePopdownAnimation", 0, 0f);
            }
        });
    }

    public void ShowConnectionError(string error) {
        mainThreadEvents.Enqueue(() => {
            GameObject connectionInfo = GameObject.FindGameObjectWithTag("ConnectionInfoMessage");
            if(!IsConnectionMessageShowing()) {
                connectionInfo.GetComponent<Animator>().Play("MessagePopupAnimation", 0, 0f);
            }
            connectionInfo.GetComponentInChildren<TextMeshProUGUI>().text = error;
            connectionInfo.GetComponentInChildren<Image>().sprite = errorSprite;
            connectionInfo.GetComponentInChildren<LoadingSpinner>().rotating = false;
            Invoke("DismissConnectionMessage", messageTTL);
        });
    }

    private bool IsConnectionMessageShowing() {
        GameObject connectionInfo = GameObject.FindGameObjectWithTag("ConnectionInfoMessage");
        return IsCurrentAnimationClip(connectionInfo.GetComponent<Animator>(), "MessagePopupAnimation");
    }

    public void SettingsToMain() {
        menuAnimator.SetBool("SettingsLoad", false);
    }

    public void SetupCharacterSelection() {
        PreselectCharacter();
    }

    private void PreselectCharacter() {
        GameObject.FindGameObjectsWithTag("CharacterSelectionContainer")[(int)currentCharacter].GetComponentInChildren<Button>().Select();
    }

    private void ChangeCharacter() {
        GameObject.FindGameObjectWithTag("CharacterName").GetComponent<TextMeshProUGUI>().text = currentCharacter.ToString();
        GameObject.FindGameObjectWithTag("CharacterSelectPreview").GetComponent<Image>().sprite = characterSprites[(int)currentCharacter];
    }

    public void CharacterSelected() {
        // todo: play character select animation
        currentMatch.selectedCharacter = currentCharacter;
    }

    public void SetupMainMenu() {
        PreselectMenuOption();

        if (IsConnectionMessageShowing()) {
            DismissConnectionMessage();
        }

        if (SNBNetwork.instance == null) {
            InitializeNetwork();
        }
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
        networkController.serverAddress = ip != "" ? ip : SNBGlobal.defaultServerIP;
    }

    public void SetNetworkPort(string port) {
        // todo: check for valid port
        networkController.port = port != "" ? Int32.Parse(port) : SNBGlobal.defaultServerPort;
    }

    private bool IsCurrentAnimationClip(Animator animator, string clipName) {
        return animator.GetCurrentAnimatorClipInfo(0).Length > 0 && 
                animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == clipName;
    }
}
