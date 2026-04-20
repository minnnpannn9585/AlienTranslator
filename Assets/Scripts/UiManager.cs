using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UiManager : MonoBehaviour
{
    [Header("Stage Intro UI")]
    [Tooltip("Stage 1/2/3 intro objects. Each object has its own animation.")]
    [SerializeField] private List<GameObject> stageIntroRoots = new List<GameObject>();

    [Header("Stage Transition UI")]
    [Tooltip("Stage 1/2/3 transition objects. Each object has its own animation.")]
    [SerializeField] private List<GameObject> stageTransitionRoots = new List<GameObject>();
    [SerializeField] private float stageTransitionDurationSeconds = 1f;

    [Header("Timer UI")]
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private TMP_Text timerText;

    [Header("Selected Card UI")]
    [SerializeField] private GameObject selectedCardTextRoot;
    [SerializeField] private TMP_Text selectedCardText;

    [Header("Play Hint UI")]
    [SerializeField] private GameObject playHintRoot;

    [Header("Play Button UI")]
    [SerializeField] private GameObject playButtonRoot;

    [Header("Result Panels")]
    [SerializeField] private GameObject correctPanel;
    [SerializeField] private GameObject wrongPanel;
    [SerializeField] private Image resultAnswerImage;

    [Header("Correct Effect")]
    [Tooltip("Shown only when the answer is correct. Can contain ParticleSystem, animation, etc.")]
    [SerializeField] private GameObject correctEffectRoot;

    [Header("Score UI")]
    [SerializeField] private Image scoreBarImage;
    [SerializeField] private RectTransform scoreIcon;
    [SerializeField] private int maxScore = 7;
    [SerializeField] private float scoreStepOffsetX = 141f;

    [Header("Warning UI")]
    [SerializeField] private GameObject warningIconRoot;
    [SerializeField] private int warningFlashCount = 3;
    [SerializeField] private float warningFlashInterval = 0.12f;

    private Coroutine _warningFlashCo;
    private GameObject _currentStageIntroRoot;
    private GameObject _currentStageTransitionRoot;
    private Vector2 _scoreIconStartAnchoredPosition;

    public float StageTransitionDurationSeconds => stageTransitionDurationSeconds;

    private void Awake()
    {
        HideResult();
        HideSelectedCardText();
        HidePlayHint();
        HidePlayButton();
        HideTimer();
        HideAllStageIntros();
        HideAllStageTransitions();
        HideWarningIcon();
        SetTimer(0f);

        if (scoreIcon != null)
            _scoreIconStartAnchoredPosition = scoreIcon.anchoredPosition;

        SetScore(maxScore);
    }

    public void PlayStageIntro(int stageNumber)
    {
        HideAllStageIntros();

        int index = stageNumber - 1;
        if (index < 0 || index >= stageIntroRoots.Count)
            return;

        _currentStageIntroRoot = stageIntroRoots[index];
        if (_currentStageIntroRoot == null)
            return;

        _currentStageIntroRoot.SetActive(false);
        _currentStageIntroRoot.SetActive(true);
    }

    public void HideCurrentStageIntro()
    {
        if (_currentStageIntroRoot != null)
            _currentStageIntroRoot.SetActive(false);

        _currentStageIntroRoot = null;
    }

    private void HideAllStageIntros()
    {
        for (int i = 0; i < stageIntroRoots.Count; i++)
        {
            var go = stageIntroRoots[i];
            if (go != null)
                go.SetActive(false);
        }

        _currentStageIntroRoot = null;
    }

    public void PlayStageTransition(int stageNumber)
    {
        HideAllStageTransitions();

        int index = stageNumber - 1;
        if (index < 0 || index >= stageTransitionRoots.Count)
            return;

        _currentStageTransitionRoot = stageTransitionRoots[index];
        if (_currentStageTransitionRoot == null)
            return;

        _currentStageTransitionRoot.SetActive(false);
        _currentStageTransitionRoot.SetActive(true);
    }

    public void HideStageTransition()
    {
        if (_currentStageTransitionRoot != null)
            _currentStageTransitionRoot.SetActive(false);

        _currentStageTransitionRoot = null;
    }

    private void HideAllStageTransitions()
    {
        for (int i = 0; i < stageTransitionRoots.Count; i++)
        {
            var go = stageTransitionRoots[i];
            if (go != null)
                go.SetActive(false);
        }

        _currentStageTransitionRoot = null;
    }

    public void ShowTimer()
    {
        if (timerRoot != null)
            timerRoot.SetActive(true);
    }

    public void HideTimer()
    {
        if (timerRoot != null)
            timerRoot.SetActive(false);
    }

    public void SetTimer(float secondsRemaining)
    {
        if (timerText == null) return;

        secondsRemaining = Mathf.Max(0f, secondsRemaining);

        int seconds = Mathf.FloorToInt(secondsRemaining);
        int centiseconds = Mathf.FloorToInt((secondsRemaining - seconds) * 100f);
        centiseconds = Mathf.Clamp(centiseconds, 0, 99);

        timerText.text = $"{seconds:00} : {centiseconds:00}";
    }

    public void ShowSelectedCardText(string content)
    {
        if (selectedCardText != null)
            selectedCardText.text = content;

        if (selectedCardTextRoot != null)
            selectedCardTextRoot.SetActive(true);
    }

    public void HideSelectedCardText()
    {
        if (selectedCardText != null)
            selectedCardText.text = string.Empty;

        if (selectedCardTextRoot != null)
            selectedCardTextRoot.SetActive(false);
    }

    public void ShowPlayHint()
    {
        if (playHintRoot != null)
            playHintRoot.SetActive(true);
    }

    public void HidePlayHint()
    {
        if (playHintRoot != null)
            playHintRoot.SetActive(false);
    }

    public void ShowPlayButton()
    {
        if (playButtonRoot != null)
            playButtonRoot.SetActive(true);
    }

    public void HidePlayButton()
    {
        if (playButtonRoot != null)
            playButtonRoot.SetActive(false);
    }

    public void SetResultAnswerSprite(Sprite sprite)
    {
        if (resultAnswerImage == null)
            return;

        resultAnswerImage.sprite = sprite;
        resultAnswerImage.enabled = sprite != null;
        resultAnswerImage.gameObject.SetActive(sprite != null);
    }

    public void ShowResult(bool correct)
    {
        if (correctPanel != null)
            correctPanel.SetActive(correct);

        if (wrongPanel != null)
            wrongPanel.SetActive(!correct);

        if (correctEffectRoot != null)
            correctEffectRoot.SetActive(correct);

        if (resultAnswerImage != null && resultAnswerImage.sprite != null)
            resultAnswerImage.gameObject.SetActive(true);
    }

    public void HideResult()
    {
        if (correctPanel != null)
            correctPanel.SetActive(false);

        if (wrongPanel != null)
            wrongPanel.SetActive(false);

        if (correctEffectRoot != null)
            correctEffectRoot.SetActive(false);

        if (resultAnswerImage != null)
        {
            resultAnswerImage.sprite = null;
            resultAnswerImage.enabled = false;
            resultAnswerImage.gameObject.SetActive(false);
        }
    }

    public IEnumerator ShowResultForSeconds(bool correct, float seconds)
    {
        ShowResult(correct);
        yield return new WaitForSeconds(seconds);
        HideResult();
    }

    public void SetScore(int score)
    {
        int clampedScore = Mathf.Clamp(score, 0, maxScore);

        if (scoreBarImage != null && maxScore > 0)
            scoreBarImage.fillAmount = (float)clampedScore / maxScore;

        if (scoreIcon != null && maxScore > 0)
        {
            int lostScore = maxScore - clampedScore;
            scoreIcon.anchoredPosition = _scoreIconStartAnchoredPosition + new Vector2(-lostScore * scoreStepOffsetX, 0f);
        }
    }

    public void FlashWarningIcon()
    {
        if (warningIconRoot == null)
            return;

        if (_warningFlashCo != null)
            StopCoroutine(_warningFlashCo);

        _warningFlashCo = StartCoroutine(FlashWarningIconRoutine());
    }

    public void HideWarningIcon()
    {
        if (warningIconRoot != null)
            warningIconRoot.SetActive(false);
    }

    private IEnumerator FlashWarningIconRoutine()
    {
        warningIconRoot.SetActive(false);

        int flashCount = Mathf.Max(1, warningFlashCount);
        float interval = Mathf.Max(0.01f, warningFlashInterval);

        for (int i = 0; i < flashCount; i++)
        {
            warningIconRoot.SetActive(true);
            yield return new WaitForSeconds(interval);

            warningIconRoot.SetActive(false);
            yield return new WaitForSeconds(interval);
        }

        _warningFlashCo = null;
    }
}