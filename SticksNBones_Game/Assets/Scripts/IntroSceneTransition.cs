using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSceneTransition : MonoBehaviour {

    ParticleSystem punchParticles;

    private void Update()
    {
        punchParticles = FindObjectOfType<ParticleSystem>();
        if (punchParticles && punchParticles.isPlaying)
        {
            FindObjectOfType<SkinnedMeshRenderer>().enabled = false;
            StartCoroutine(ToNextScene());
        }
    }

    private IEnumerator ToNextScene()
    {
        yield return new WaitForSeconds(1.0f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
