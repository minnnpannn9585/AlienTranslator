using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameFlowController : MonoBehaviour
{
    [SerializeField] private HandController hand;
    [SerializeField] private UiManager ui;

    [Header("Rounds (fixed data)")]
    [SerializeField] private List<RoundConfig> rounds = new List<RoundConfig>();

    [Header("Stage Settings")]
    [SerializeField] private int totalStages = 3;
    [SerializeField] private int roundsPerStage = 6;

    [Header("Score")]
    [SerializeField] private int initialScore = 7;

    [Header("Result")]
    [SerializeField] private float resultPanelDurationSeconds = 2f;

    [Header("Timer")]
    [SerializeField] private float roundTimeSeconds = 10f;

    [Header("Character Animation Roots")]
    [SerializeField] private Transform characterARoot;
    [SerializeField] private Transform characterBRoot;

    [Tooltip("How long to wait after character objects appear before dealing cards.")]
    [SerializeField] private float characterShowSeconds = 2f;

    [Header("Endings")]
    [SerializeField] private EndingData ending1;
    [SerializeField] private EndingData ending2;
    [SerializeField] private EndingData ending3;
    [SerializeField] private EndingData ending4;
    [SerializeField] private EndingData ending5;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmAudioSource;

    private AudioClip _gameBgmClip;
    private float _gameBgmTime;

    private int _roundIndex = -1;
    private int _currentStage = 0;
    private int _roundInCurrentStage = 0;
    private int _score;
    private int _usedAngryCardCount;

    private CardDefinition _lastPlayedCard;

    private Coroutine _stageFlowCo;
    private Coroutine _roundFlowCo;
    private Coroutine _timerCo;

    private bool _roundInProgress;
    private bool _gameEnded;

    private RoundConfig _activeRound;

    private readonly List<GameObject> _spawnedCharacterAObjects = new();
    private readonly List<GameObject> _spawnedCharacterBObjects = new();

    private void OnEnable()
    {
        if (hand != null)
            hand.Played += OnHandPlayed;
    }

    private void OnDisable()
    {
        if (hand != null)
            hand.Played -= OnHandPlayed;
    }

    private void Start()
    {
        ClearSpawnedCharacters();

        _score = initialScore;
        _usedAngryCardCount = 0;
        _lastPlayedCard = null;
        _gameEnded = false;

        RestoreGameBgm();

        if (ui != null)
        {
            ui.HideTimer();
            ui.HideEnding();
            ui.SetScore(_score);
        }

        StartNextStage();
    }

    private void RestoreGameBgm()
    {
        if (bgmAudioSource == null)
            return;

        if (_gameBgmClip == null)
            _gameBgmClip = bgmAudioSource.clip;

        bgmAudioSource.clip = _gameBgmClip;
        bgmAudioSource.loop = true;

        if (!bgmAudioSource.isPlaying)
            bgmAudioSource.Play();
    }

    private void StartNextStage()
    {
        if (_gameEnded)
            return;

        if (_currentStage >= totalStages)
        {
            TriggerFinalEnding();
            return;
        }

        if (_stageFlowCo != null)
            StopCoroutine(_stageFlowCo);

        _stageFlowCo = StartCoroutine(RunStageIntroThenBegin());
    }

    private IEnumerator RunStageIntroThenBegin()
    {
        StopTimer();

        if (hand != null)
        {
            hand.SetInteractable(false);
            hand.ClearHand();
        }

        ClearSpawnedCharacters();

        if (ui != null)
        {
            ui.HideTimer();
            ui.HideResult();
            ui.SetTimer(roundTimeSeconds);
            ui.PlayStageIntro(_currentStage + 1);
        }

        yield return WaitForAnyKeyDown();

        if (ui != null)
        {
            ui.HideCurrentStageIntro();
            ui.PlayStageTransition(_currentStage + 1);
        }

        SfxManager.Instance?.PlayStageIntro();

        yield return new WaitForSeconds(ui != null ? ui.StageTransitionDurationSeconds : 1f);

        if (ui != null)
            ui.HideStageTransition();

        _roundInCurrentStage = 0;

        StartNextRoundFlow();
        _stageFlowCo = null;
    }

    private IEnumerator WaitForAnyKeyDown()
    {
        while (!Input.anyKeyDown)
            yield return null;
    }

    private void StartNextRoundFlow()
    {
        if (_gameEnded)
            return;

        if (_roundFlowCo != null)
            StopCoroutine(_roundFlowCo);

        _roundFlowCo = StartCoroutine(RunRoundFlow());
    }

    private IEnumerator RunRoundFlow()
    {
        if (hand == null) yield break;
        if (rounds == null || rounds.Count == 0)
        {
            Debug.LogError($"{nameof(GameFlowController)}: rounds is empty.");
            yield break;
        }

        _roundInProgress = true;

        StopTimer();
        hand.SetInteractable(false);
        hand.ClearHand();
        ClearSpawnedCharacters();

        if (ui != null)
            ui.HideTimer();

        _roundIndex++;
        _roundInCurrentStage++;

        if (_roundIndex >= rounds.Count)
        {
            TriggerFinalEnding();
            yield break;
        }

        _activeRound = rounds[_roundIndex];

        if (_activeRound == null)
        {
            Debug.LogError($"{nameof(GameFlowController)}: round is null.");
            _roundInProgress = false;
            yield break;
        }

        if (ui != null)
        {
            ui.HideResult();
            ui.SetTimer(roundTimeSeconds);
        }

        SpawnRoundCharacters(_activeRound);

        yield return new WaitForSeconds(characterShowSeconds);

        hand.SetRound(_activeRound);
        hand.DealCurrentRound();
        hand.SetInteractable(true);

        if (ui != null)
            ui.ShowTimer();

        StartTimer();
    }

    private void OnHandPlayed(bool correct, CardDefinition playedCard)
    {
        StopTimer();

        if (ui != null)
            ui.HideTimer();

        _lastPlayedCard = playedCard;

        if (playedCard != null && playedCard.IsAngryCard)
            _usedAngryCardCount++;

        if (_usedAngryCardCount >= 6)
        {
            TriggerEnding(ending3);
            return;
        }

        if (correct)
            SfxManager.Instance?.PlayCorrect();
        else
            SfxManager.Instance?.PlayWrong();

        if (!correct)
            ApplyWrongPenalty();

        if (_roundFlowCo != null)
        {
            StopCoroutine(_roundFlowCo);
            _roundFlowCo = null;
        }

        bool isLastRound = _roundIndex >= rounds.Count - 1;
        if (isLastRound)
        {
            TriggerFinalEnding();
            return;
        }

        var correctCard = _activeRound != null ? _activeRound.GetCorrectCardDefinition() : null;
        if (ui != null)
            ui.SetResultAnswerSprite(correctCard != null ? correctCard.Artwork : null);

        StartCoroutine(ShowResultThenContinue(correct ? UiManager.ResultType.Correct : UiManager.ResultType.Wrong));
    }

    private IEnumerator ShowResultThenContinue(UiManager.ResultType resultType)
    {
        if (hand != null)
            hand.SetInteractable(false);

        if (ui != null)
        {
            ui.ShowResult(resultType);
            yield return new WaitForSeconds(resultPanelDurationSeconds);
            ui.HideResult();
        }
        else
        {
            yield return new WaitForSeconds(resultPanelDurationSeconds);
        }

        if (_score <= 0)
        {
            TriggerEnding(ending2);
            yield break;
        }

        _roundInProgress = false;

        if (_roundInCurrentStage >= roundsPerStage)
        {
            _currentStage++;
            StartNextStage();
        }
        else
        {
            StartNextRoundFlow();
        }
    }

    private void StartTimer()
    {
        if (_timerCo != null)
            StopCoroutine(_timerCo);

        _timerCo = StartCoroutine(RoundTimerRoutine());
    }

    private void StopTimer()
    {
        if (_timerCo == null) return;
        StopCoroutine(_timerCo);
        _timerCo = null;
    }

    private IEnumerator RoundTimerRoutine()
    {
        float remaining = Mathf.Max(0.1f, roundTimeSeconds);

        while (remaining > 0f)
        {
            if (ui != null)
                ui.SetTimer(remaining);

            remaining -= Time.deltaTime;
            yield return null;
        }

        _timerCo = null;

        if (ui != null)
        {
            ui.SetTimer(0f);
            ui.HideTimer();
        }

        ApplyWrongPenalty();
        SfxManager.Instance?.PlayWrong();

        bool isLastRound = _roundIndex >= rounds.Count - 1;
        if (isLastRound)
        {
            TriggerFinalEnding();
            yield break;
        }

        var correctCard = _activeRound != null ? _activeRound.GetCorrectCardDefinition() : null;
        if (ui != null)
            ui.SetResultAnswerSprite(correctCard != null ? correctCard.Artwork : null);

        if (_roundFlowCo != null)
        {
            StopCoroutine(_roundFlowCo);
            _roundFlowCo = null;
        }

        yield return ShowResultThenContinue(UiManager.ResultType.Timeout);
    }

    private void ApplyWrongPenalty()
    {
        _score = Mathf.Max(0, _score - 1);

        if (ui != null)
        {
            ui.SetScore(_score);
            ui.FlashWarningIcon();
        }
    }

    private void TriggerFinalEnding()
    {
        if (_score <= 0)
        {
            TriggerEnding(ending2);
            return;
        }

        if (_usedAngryCardCount >= 6)
        {
            TriggerEnding(ending3);
            return;
        }

        int lastId = _lastPlayedCard != null ? _lastPlayedCard.Id : -1;

        if (lastId == 105)
        {
            TriggerEnding(ending4);
            return;
        }

        if (lastId == 103)
        {
            TriggerEnding(ending1);
            return;
        }

        if (lastId == 104 || lastId == 102 || lastId == 101 || lastId == 100)
        {
            TriggerEnding(ending5);
            return;
        }

        TriggerEnding(ending5);
    }

    private void TriggerEnding(EndingData endingData)
    {
        if (_gameEnded)
            return;

        _gameEnded = true;
        _roundInProgress = false;

        StopTimer();

        if (_stageFlowCo != null)
        {
            StopCoroutine(_stageFlowCo);
            _stageFlowCo = null;
        }

        if (_roundFlowCo != null)
        {
            StopCoroutine(_roundFlowCo);
            _roundFlowCo = null;
        }

        if (hand != null)
            hand.SetInteractable(false);

        ClearSpawnedCharacters();

        if (bgmAudioSource != null)
        {
            if (_gameBgmClip == null)
                _gameBgmClip = bgmAudioSource.clip;

            _gameBgmTime = bgmAudioSource.time;

            if (endingData != null && endingData.EndingBgm != null)
            {
                bgmAudioSource.clip = endingData.EndingBgm;
                bgmAudioSource.loop = false;
                bgmAudioSource.Play();
            }
        }

        if (ui != null)
        {
            ui.HideTimer();
            ui.HideResult();
            ui.ShowEnding(endingData);
        }
    }

    private void SpawnRoundCharacters(RoundConfig round)
    {
        ClearSpawnedCharacters();

        if (round == null)
            return;

        SpawnCharacterGroup(round.CharacterPrefabs, characterARoot, _spawnedCharacterAObjects);
        SpawnCharacterGroup(round.CharacterBPrefabs, characterBRoot, _spawnedCharacterBObjects);
    }

    private static void SpawnCharacterGroup(
        IReadOnlyList<GameObject> prefabs,
        Transform root,
        List<GameObject> spawnedObjects)
    {
        if (prefabs == null)
            return;

        for (int i = 0; i < prefabs.Count; i++)
        {
            var prefab = prefabs[i];
            if (prefab == null) continue;

            var instance = root != null
                ? Instantiate(prefab, root)
                : Instantiate(prefab);

            spawnedObjects.Add(instance);
        }
    }

    private void ClearSpawnedCharacters()
    {
        ClearSpawnedGroup(_spawnedCharacterAObjects);
        ClearSpawnedGroup(_spawnedCharacterBObjects);
    }

    private static void ClearSpawnedGroup(List<GameObject> spawnedObjects)
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            var go = spawnedObjects[i];
            if (go != null)
                Destroy(go);
        }

        spawnedObjects.Clear();
    }
}