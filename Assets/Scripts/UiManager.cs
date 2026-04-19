using System.Collections;
using TMPro;
using UnityEngine;

public sealed class UiManager : MonoBehaviour
{
    [Header("Round UI")]
    [SerializeField] private TMP_Text roundText;

    [Tooltip("Root GameObject that has the Animator for round intro. Enable = auto play.")]
    [SerializeField] private GameObject roundIntroRoot;

    [Tooltip("Round intro animation duration in seconds.")]
    [SerializeField] private float roundIntroDurationSeconds = 2f;

    [Header("Timer UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Result Panels")]
    [SerializeField] private GameObject correctPanel;
    [SerializeField] private GameObject wrongPanel;

    private void Awake()
    {
        HideResult();
        SetTimer(0f);

        if (roundIntroRoot != null)
            roundIntroRoot.SetActive(false);
    }

    public float RoundIntroDurationSeconds => roundIntroDurationSeconds;

    public void SetRound(int roundNumber)
    {
        if (roundText != null)
            roundText.text = $"Round: {roundNumber}";
    }

    /// <summary>
    /// Replays the round intro animation by toggling the animated GameObject off/on.
    /// Animator should auto-play its default state when enabled.
    /// </summary>
    public void PlayRoundIntro(int roundNumber)
    {
        SetRound(roundNumber);

        if (roundIntroRoot == null)
            return;

        // Force replay
        roundIntroRoot.SetActive(false);
        roundIntroRoot.SetActive(true);
    }

    // Format: "xx : yy"
    // xx = seconds (integer)
    // yy = centiseconds (0-99)
    public void SetTimer(float secondsRemaining)
    {
        if (timerText == null) return;

        secondsRemaining = Mathf.Max(0f, secondsRemaining);

        int seconds = Mathf.FloorToInt(secondsRemaining);
        int centiseconds = Mathf.FloorToInt((secondsRemaining - seconds) * 100f);
        centiseconds = Mathf.Clamp(centiseconds, 0, 99);

        timerText.text = $"{seconds:00} : {centiseconds:00}";
    }

    public void ShowResult(bool correct)
    {
        if (correctPanel != null) correctPanel.SetActive(correct);
        if (wrongPanel != null) wrongPanel.SetActive(!correct);
    }

    public void HideResult()
    {
        if (correctPanel != null) correctPanel.SetActive(false);
        if (wrongPanel != null) wrongPanel.SetActive(false);
    }

    public IEnumerator ShowResultForSeconds(bool correct, float seconds)
    {
        ShowResult(correct);
        yield return new WaitForSeconds(seconds);
        HideResult();
    }
}