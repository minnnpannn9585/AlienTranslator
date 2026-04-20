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

    private int _roundIndex = -1;
    private int _currentStage = 0;
    private int _roundInCurrentStage = 0;
    private int _score;

    private Coroutine _stageFlowCo;
    private Coroutine _roundFlowCo;
    private Coroutine _timerCo;

    private bool _roundInProgress;
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

        if (ui != null)
        {
            ui.HideTimer();
            ui.SetScore(_score);
        }

        StartNextStage();
    }

    private void StartNextStage()
    {
        if (_currentStage >= totalStages)
        {
            Debug.Log("Game Finished.");
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

        // 新 stage 开始前清理上一轮残留的角色动画物体
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

        // 下回合真正开始时，再清理上一回合的角色动画物体
        ClearSpawnedCharacters();

        if (ui != null)
            ui.HideTimer();

        _roundIndex++;
        _roundInCurrentStage++;

        if (_roundIndex >= rounds.Count)
        {
            Debug.Log("All rounds finished.");
            _roundInProgress = false;
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

    private void OnHandPlayed(bool correct)
    {
        StopTimer();

        if (ui != null)
            ui.HideTimer();

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

        StartCoroutine(ShowResultThenContinue(correct));
    }

    private IEnumerator ShowResultThenContinue(bool correct)
    {
        if (hand != null)
            hand.SetInteractable(false);

        // 这里不再清理角色动画物体，让它保留到下一回合开始
        if (ui != null)
            yield return ui.ShowResultForSeconds(correct, resultPanelDurationSeconds);
        else
            yield return new WaitForSeconds(resultPanelDurationSeconds);

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

        if (ui != null)
        {
            ui.SetTimer(0f);
            ui.HideTimer();
        }

        StopTimer();

        ApplyWrongPenalty();
        SfxManager.Instance?.PlayWrong();

        if (_roundFlowCo != null)
        {
            StopCoroutine(_roundFlowCo);
            _roundFlowCo = null;
        }

        yield return ShowResultThenContinue(false);
        _timerCo = null;
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