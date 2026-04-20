using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [Header("Start Transition")]
    [SerializeField] private GameObject startTransitionObject;
    [SerializeField] private float startTransitionDuration = 20f;

    private bool _isStarting;

    public void OnClickStart()
    {
        if (_isStarting)
            return;

        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        _isStarting = true;

        if (startTransitionObject != null)
        {
            startTransitionObject.SetActive(false);
            startTransitionObject.SetActive(true);
        }

        yield return new WaitForSeconds(startTransitionDuration);

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("No next scene found in Build Settings.");
            _isStarting = false;
            yield break;
        }

        SceneManager.LoadScene(nextSceneIndex);
    }

    public void OnClickQuit()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("Quit is not supported on WebGL.");
#elif UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}