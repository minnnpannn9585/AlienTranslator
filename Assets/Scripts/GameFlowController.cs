using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameFlowController : MonoBehaviour
{
    [SerializeField] private HandController hand;
    [SerializeField] private UiManager ui;

    [Header("Rounds (fixed data)")]
    [SerializeField] private List<RoundConfig> rounds = new List<RoundConfig>();

    [Header("Input")]
    [SerializeField] private KeyCode startKey = KeyCode.Space;

    [Header("Result")]
    [SerializeField] private float resultPanelDurationSeconds = 3f;

    [Header("Timer")]
    [SerializeField] private float roundTimeSeconds = 10f;

    [Header("Character Animation")]
    [SerializeField] private Transform characterRoot;

    [Tooltip("How long to wait after character objects appear before dealing cards.")]
    [SerializeField] private float characterShowSeconds = 2f;

    private int _roundIndex = -1;
    private Coroutine _roundFlowCo;
    private Coroutine _timerCo;

    private bool _roundInProgress;
    private RoundConfig _activeRound;

    private readonly List<GameObject> _spawnedCharacters = new();

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
    }

    private void Update()
    {
        if (Input.GetKeyDown(startKey) && !_roundInProgress)
            StartNextRoundFlow();
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
        ClearSpawnedCharacters();

        _roundIndex = (_roundIndex + 1) % rounds.Count;
        _activeRound = rounds[_roundIndex];

        if (_activeRound == null)
        {
            Debug.LogError($"{nameof(GameFlowController)}: round is null.");
            yield break;
        }

        // 1. Round 文字动画
        if (ui != null)
        {
            ui.HideResult();
            ui.SetTimer(roundTimeSeconds);
            ui.PlayRoundIntro(_roundIndex + 1);
        }

        float introWait = ui != null ? ui.RoundIntroDurationSeconds : 2f;
        yield return new WaitForSeconds(introWait);

        // 2. 生成本轮角色动画物体，并保持到回合结束
        SpawnRoundCharacters(_activeRound);

        // 3. 等待 2 秒后开始发牌
        yield return new WaitForSeconds(characterShowSeconds);

        hand.SetRound(_activeRound);
        hand.DealCurrentRound();
        hand.SetInteractable(true);

        StartTimer();
    }

    private void OnHandPlayed(bool correct)
    {
        StopTimer();

        if (_roundFlowCo != null)
        {
            StopCoroutine(_roundFlowCo);
            _roundFlowCo = null;
        }

        StartCoroutine(ShowResultThenNextRound(correct));
    }

    private IEnumerator ShowResultThenNextRound(bool correct)
    {
        if (hand != null)
            hand.SetInteractable(false);

        // 回合结束时再清理本轮角色物体
        ClearSpawnedCharacters();

        if (ui != null)
            yield return ui.ShowResultForSeconds(correct, resultPanelDurationSeconds);
        else
            yield return new WaitForSeconds(resultPanelDurationSeconds);

        _roundInProgress = false;
        StartNextRoundFlow();
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
            ui.SetTimer(0f);

        StopTimer();

        if (_roundFlowCo != null)
        {
            StopCoroutine(_roundFlowCo);
            _roundFlowCo = null;
        }

        yield return ShowResultThenNextRound(false);
        _timerCo = null;
    }

    private void SpawnRoundCharacters(RoundConfig round)
    {
        ClearSpawnedCharacters();

        if (round == null || round.CharacterPrefabs == null)
            return;

        for (int i = 0; i < round.CharacterPrefabs.Count; i++)
        {
            var prefab = round.CharacterPrefabs[i];
            if (prefab == null) continue;

            var instance = Instantiate(prefab, characterRoot);
            _spawnedCharacters.Add(instance);
        }
    }

    private void ClearSpawnedCharacters()
    {
        for (int i = 0; i < _spawnedCharacters.Count; i++)
        {
            var go = _spawnedCharacters[i];
            if (go != null)
                Destroy(go);
        }

        _spawnedCharacters.Clear();
    }
}