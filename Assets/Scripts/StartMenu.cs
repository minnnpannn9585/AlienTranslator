using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [Header("Intro Canvas Sequence")]
    [SerializeField] private GameObject firstIntroCanvas;
    [SerializeField] private float firstIntroMinimumDuration = 10f;
    [SerializeField] private GameObject secondIntroCanvas;
    [SerializeField] private float secondIntroMinimumDuration = 10f;

    private bool _isStarting;
    private bool _canAdvanceIntro;
    private bool _advanceRequested;

    private void Awake()
    {
        if (firstIntroCanvas != null)
            firstIntroCanvas.SetActive(false);

        if (secondIntroCanvas != null)
            secondIntroCanvas.SetActive(false);
    }

    private void Update()
    {
        if (!_isStarting || !_canAdvanceIntro)
            return;

        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown)
            _advanceRequested = true;
    }

    public void OnClickStart()
    {
        if (_isStarting)
            return;

        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        _isStarting = true;

        yield return PlayIntroAndWaitForClick(firstIntroCanvas, firstIntroMinimumDuration);

        if (firstIntroCanvas != null)
            firstIntroCanvas.SetActive(false);

        yield return PlayIntroAndWaitForClick(secondIntroCanvas, secondIntroMinimumDuration);

        LoadNextScene();
    }

    private IEnumerator PlayIntroAndWaitForClick(GameObject introCanvas, float minimumDuration)
    {
        _advanceRequested = false;
        _canAdvanceIntro = false;

        if (introCanvas != null)
        {
            introCanvas.SetActive(false);
            introCanvas.SetActive(true);
        }

        yield return new WaitForSeconds(minimumDuration);

        _canAdvanceIntro = true;

        while (!_advanceRequested)
            yield return null;

        _canAdvanceIntro = false;
        _advanceRequested = false;
    }

    private void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("No next scene found in Build Settings.");
            _isStarting = false;
            return;
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