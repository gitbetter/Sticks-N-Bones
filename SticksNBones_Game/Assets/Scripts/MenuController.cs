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
    [SerializeField] AudioClip buttonHighlightAudio;
    [SerializeField] AudioClip buttonSelectAudio;
    [SerializeField] AudioClip characterSelectAudio;
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
    [SerializeField] MatchHandler matchHandler;

    private enum MenuScreens { None, First, Main, Settings, Character };
    private enum ConnectionMessageState { Loading, Success, Error };

    MenuScreens currentScreen = MenuScreens.None;
    Queue<Action> mainThreadEvents = new Queue<Action>();
    CharacterType highlightedCharacter = CharacterType.Classico;
    Resolution[] resolutions;
    MatchHandler currentMatch;

    Animator menuAnimator;
    AudioSource buttonSFXSource;

    private void Awake() {
        menuAnimator = GetComponent<Animator>();
        buttonSFXSource = NewAudioSource(false, false, 0.75f);
    }

    private void Update() {
        NavigateIntroMenu();
        DispatchActions();
        UpdateHighlightedCharacter();
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

    private AudioSource NewAudioSource(bool loops, bool playsOnAwake, float volume) {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = loops;
        audioSource.playOnAwake = playsOnAwake;
        audioSource.volume = volume;
        return audioSource;
    }

    private void DispatchActions() {
        while (mainThreadEvents.Count > 0) {
            Action action = mainThreadEvents.Dequeue();
            action();
        }
    }

//       _____ _             _               
//      /  ___| |           | |              
//      \ `--.| |_ __ _ _ __| |_ _   _ _ __  
//       `--. \ __/ _` | '__| __| | | | '_ \ 
//      /\__/ / || (_| | |  | |_| |_| | |_) |
//      \____/ \__\__,_|_|   \__|\__,_| .__/ 
//                                  | |    
//                                  |_|

    private void NavigateIntroMenu() {
        if (Input.anyKeyDown && currentScreen == MenuScreens.First) {
            GoToMainMenu();
        }
    }

    public void LoadStart() {
        currentScreen = MenuScreens.First;
        menuAnimator.SetBool("FirstLoad", true);
    }

//    ___  ___      _        ___  ___                 
//    |  \/  |     (_)       |  \/  |                 
//    | .  . | __ _ _ _ __   | .  . | ___ _ __ _   _ 
//    | |\/| |/ _` | | '_ \  | |\/| |/ _ \ '_ \| | | |
//    | |  | | (_| | | | | | | |  | |  __/ | | | |_| |
//    \_|  |_/\__,_|_|_| |_| \_|  |_/\___|_| |_|\__,_|

    public void GoToMainMenu() {
        currentScreen = MenuScreens.Main;
        menuAnimator.SetBool("MainLoad", true);
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

    public void PlayQuickMatch() {
        GoToCharacterSelect();
        networkController.GetRandomMatch((response) => {
            mainThreadEvents.Enqueue(() => {
                if (response.Count > 0) {
                    string oppIp, oppUsername; int oppPort; bool is_hosting;

                    currentMatch = Instantiate(matchHandler);
                    response.GetField(out oppIp, "ip", null);
                    response.GetField(out oppPort, "port", -1);
                    response.GetField(out is_hosting, "is_hosting", false);
                    response.GetField(out oppUsername, "username", null);

                    currentMatch.isServer = is_hosting;
                    currentMatch.opponentIp = oppIp;
                    currentMatch.opponentPort = oppPort;
                    currentMatch.opponent.username = oppUsername;

                    currentMatch.OnOpponentConnect += OpponentConnected;
                    currentMatch.OnOpponentDisconnect += OpponentDisconnected;
                    currentMatch.OnOpponentReady += OpponentBecameReady;
                    currentMatch.OnMatchTransition += NextMatchStage;

                    FlipConnectionMessage();
                    ShowConnectionLoad("Waiting for " + oppUsername);
                }
            });
        });
    }

    public void ShowLeaderboards() {
        // todo
    }

    public void ShowSettings() {
        GoToSettings();
    }

    public void QuitGame() {
        if (currentMatch != null) {
            currentMatch.LeaveMatch();
        }
        networkController.TerminateConnection((response) => {
            Debug.Log("Quitting...");
            Application.Quit();
        });
    }

//     _____ _                          _              _____      _           _   _             
//    /  __ \ |                        | |            /  ___|    | |         | | (_)            
//    | /  \/ |__   __ _ _ __ __ _  ___| |_ ___ _ __  \ `--.  ___| | ___  ___| |_ _  ___  _ __  
//    | |   | '_ \ / _` | '__/ _` |/ __| __/ _ \ '__|  `--. \/ _ \ |/ _ \/ __| __| |/ _ \| '_ \ 
//    | \__/\ | | | (_| | | | (_| | (__| ||  __/ |    /\__/ /  __/ |  __/ (__| |_| | (_) | | | |
//     \____/_| |_|\__,_|_|  \__,_|\___|\__\___|_|    \____/ \___|_|\___|\___|\__|_|\___/|_| |_|


    public void GoToCharacterSelect() {
        currentScreen = MenuScreens.Character;
        menuAnimator.SetBool("CharacterSelectLoad", true);
    }

    public void SetupCharacterSelection() {
        PreselectCharacter();
    }

    private void PreselectCharacter() {
        GameObject.FindGameObjectsWithTag("CharacterSelectionContainer")[(int)highlightedCharacter].GetComponentInChildren<Button>().Select();
    }

    private void ChangeCharacter() {
        GameObject.FindGameObjectWithTag("CharacterName").GetComponent<TextMeshProUGUI>().text = highlightedCharacter.ToString();
        GameObject.FindGameObjectWithTag("CharacterSelectPreview").GetComponent<Image>().sprite = characterSprites[(int)highlightedCharacter];
    }

    public void CharacterSelected() {
        menuAnimator.Play("CharacterSelectedAnimation", 0, 0f);
        PlayCharacterSelectAudio();

        GameObject[] selectionContainers = GameObject.FindGameObjectsWithTag("CharacterSelectionContainer");
        for (int i = 0; i < selectionContainers.Length; i++) {
            GameObject cont = selectionContainers[i];
            if (cont.GetComponentInChildren<Button>().gameObject == EventSystem.current.currentSelectedGameObject) {
                if ((int)highlightedCharacter != i) {
                    // todo: do something nice
                }
            } else {
                // todo: undo the niceness
            }
        }

        SNBGlobal.thisPlayer.character = highlightedCharacter;
        if (currentMatch != null) {
            currentMatch.PlayerReady();
        }
    }

    private void UpdateHighlightedCharacter() {
        GameObject[] selectionContainers = GameObject.FindGameObjectsWithTag("CharacterSelectionContainer");
        for (int i = 0; i < selectionContainers.Length; i++) {
            GameObject cont = selectionContainers[i];
            if (cont.GetComponentInChildren<Button>().gameObject == EventSystem.current.currentSelectedGameObject) {
                cont.GetComponent<Image>().color = new Color(1f, 0.9896f, 0);
                if ((int)highlightedCharacter != i) {
                    highlightedCharacter = (CharacterType)i;
                    menuAnimator.Play("CharacterSelectionChangedAnimation", -1, 0f);
                }
            } else {
                cont.GetComponent<Image>().color = new Color(1f, 1f, 1f);
            }
        }
    }

    public void OpponentDisconnected() {
        FlipConnectionMessage();
        ShowConnectionLoad("Waiting for opponent");
    }

    public void OpponentBecameReady() {
        currentMatch.opponent.state = PlayerState.Ready;
        ShowConnectionSuccess(currentMatch.opponent.username + " ready to fight!");
        if (SNBGlobal.thisPlayer.state == PlayerState.Ready) {
            NextMatchStage();
        }
    }

    public void OpponentConnected() { }

    public void NextMatchStage() {
        // todo: time to fight!
    }

    public void CharacterSelectToMain() {
        currentScreen = MenuScreens.Main;
        menuAnimator.SetBool("CharacterSelectLoad", false);
        if (currentMatch != null) {
            currentMatch.LeaveMatch();
            currentMatch = null;
        }
    }

//     _____      _   _   _                 
//    /  ___|    | | | | (_)                
//    \ `--.  ___| |_| |_ _ _ __   __ _ ___ 
//     `--. \/ _ \ __| __| | '_ \ / _` / __|
//    /\__/ /  __/ |_| |_| | | | | (_| \__ \
//    \____/ \___|\__|\__|_|_| |_|\__, |___/
//                                 __/ |    
//                                |___/     

    public void GoToSettings() {
        currentScreen = MenuScreens.Settings;
        menuAnimator.SetBool("SettingsLoad", true);
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

    public void SettingsToMain() {
        menuAnimator.SetBool("SettingsLoad", false);
    }

//      ___            _ _       
//     / _ \          | (_)      
//    / /_\ \_   _  __| |_  ___  
//    |  _  | | | |/ _` | |/ _ \ 
//    | | | | |_| | (_| | | (_) |
//    \_| |_/\__,_|\__,_|_|\___/ 


    public void PlayLogoCrash() {
        GetComponent<AudioSource>().PlayOneShot(logoCrashAudio);
    }

    public void PlayMenuMusic() {
        GetComponent<AudioSource>().PlayOneShot(mainMenuMusic, menuMusicVolume);
    }

    public void PlayButtonHighlightAudio() {
        buttonSFXSource.PlayOneShot(buttonHighlightAudio);
    }

    public void PlayButtonSelectedAudio() {
        buttonSFXSource.PlayOneShot(buttonSelectAudio);
    }

    public void PlayCharacterSelectAudio() {
        buttonSFXSource.PlayOneShot(characterSelectAudio);
    }

//     _____                             _   _              ___  ___                               
//    /  __ \                           | | (_)             |  \/  |                               
//    | /  \/ ___  _ __  _ __   ___  ___| |_ _  ___  _ __   | .  . | ___  ___ ___  __ _  __ _  ___ 
//    | |    / _ \| '_ \| '_ \ / _ \/ __| __| |/ _ \| '_ \  | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \
//    | \__/\ (_) | | | | | | |  __/ (__| |_| | (_) | | | | | |  | |  __/\__ \__ \ (_| | (_| |  __/
//     \____/\___/|_| |_|_| |_|\___|\___|\__|_|\___/|_| |_| \_|  |_/\___||___/___/\__,_|\__, |\___|
//                                                                                       __/ |     
//                                                                                      |___/      

    public void ShowConnectionLoad(string message) {
        mainThreadEvents.Enqueue(() => {
            CancelInvoke("DismissConnectionMessage");
            GameObject connectionInfo = GameObject.FindGameObjectWithTag("ConnectionInfoMessage");
            if (!IsConnectionMessageShowing()) {
                connectionInfo.GetComponent<Animator>().Play("MessagePopupAnimation", 0, 0f);
            }
            UpdateConnectionMessage(message, ConnectionMessageState.Loading);
        });
    }

    public void ShowConnectionSuccess(string message) {
        mainThreadEvents.Enqueue(() => {
            CancelInvoke("DismissConnectionMessage");
            GameObject connectionInfo = GameObject.FindGameObjectWithTag("ConnectionInfoMessage");
            connectionInfo.GetComponent<Animator>().Play("MessageAlertAnimation", 0, 0f);
            UpdateConnectionMessage(message, ConnectionMessageState.Success);
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
            CancelInvoke("DismissConnectionMessage");
            GameObject connectionInfo = GameObject.FindGameObjectWithTag("ConnectionInfoMessage");
            if (!IsConnectionMessageShowing()) {
                connectionInfo.GetComponent<Animator>().Play("MessagePopupAnimation", 0, 0f);
            }
            UpdateConnectionMessage(error, ConnectionMessageState.Error);
            Invoke("DismissConnectionMessage", messageTTL);
        });
    }

    private bool IsConnectionMessageShowing() {
        GameObject connectionInfo = GameObject.FindGameObjectWithTag("ConnectionInfoMessage");
        return IsCurrentAnimationClip(connectionInfo.GetComponent<Animator>(), "MessagePopupAnimation");
    }

    private void UpdateConnectionMessage(string message, ConnectionMessageState state) {
        GameObject[] connectionInfoContainers = GameObject.FindGameObjectsWithTag("InfoMessageStateContainer");
        foreach (GameObject cont in connectionInfoContainers) {
            if (cont.GetComponent<CanvasGroup>().alpha == 1.0f) {
                cont.GetComponentInChildren<TextMeshProUGUI>().text = message;
                cont.GetComponentInChildren<Image>().sprite = state == ConnectionMessageState.Error ? errorSprite :
                                                                        state == ConnectionMessageState.Success ? successSprite : loadingSprite;
                cont.GetComponentInChildren<LoadingSpinner>().rotating = state == ConnectionMessageState.Loading;
            }
        }
    }

    private void FlipConnectionMessage() {
        GameObject[] connectionInfoContainers = GameObject.FindGameObjectsWithTag("InfoMessageStateContainer");
        foreach (GameObject cont in connectionInfoContainers) {
            if (cont.GetComponent<CanvasGroup>().alpha == 0) {
                cont.GetComponent<CanvasGroup>().alpha = 1.0f;
            } else {
                cont.GetComponent<CanvasGroup>().alpha = 0;
            }
        }
    }

//    ___  ____              _ _                                       ___  
//    |  \/  (_)            | | |                                     |__ \ 
//    | .  . |_ ___  ___ ___| | | __ _ _ __   ___  __ _  ___  _   _ ___  ) |
//    | |\/| | / __|/ __/ _ \ | |/ _` | '_ \ / _ \/ _` |/ _ \| | | / __|/ / 
//    | |  | | \__ \ (_|  __/ | | (_| | | | |  __/ (_| | (_) | |_| \__ \_|  
//    \_|  |_/_|___/\___\___|_|_|\__,_|_| |_|\___|\__,_|\___/ \__,_|___(_)

    private bool IsCurrentAnimationClip(Animator animator, string clipName) {
        return animator.GetCurrentAnimatorClipInfo(0).Length > 0 &&
                animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == clipName;
    }
}
