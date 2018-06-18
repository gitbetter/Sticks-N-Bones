using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

using TMPro;

public class VersusScreenController : MonoBehaviour {

    [SerializeField] Image firstPlayerImage;
    [SerializeField] Image secondPlayerImage;
    [SerializeField] TextMeshProUGUI firstPlayerName;
    [SerializeField] TextMeshProUGUI secondPlayerName;

    private MatchHandler matchHandler;

	private void Start () {
        matchHandler = FindObjectOfType<MatchHandler>();

        firstPlayerName.text = SNBGlobal.thisUser.character.ToString();
        secondPlayerName.text = matchHandler.opponent.character.ToString();
        firstPlayerImage.sprite = matchHandler.characterSprites[(int)SNBGlobal.thisUser.character];
        secondPlayerImage.sprite = matchHandler.characterSprites[(int)matchHandler.opponent.character];
	}

    public void ContinueToMatch() {
        FindObjectOfType<Camera>().transform.parent.GetComponent<PlayableDirector>().Play();
        Destroy(gameObject);
    }
}
