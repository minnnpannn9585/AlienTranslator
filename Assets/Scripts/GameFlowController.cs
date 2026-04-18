using System.Collections.Generic;
using UnityEngine;

public sealed class GameFlowController : MonoBehaviour
{
    [SerializeField] private HandController hand;
    [SerializeField] private RoundHud hud;

    [Header("Rounds (fixed data)")]
    [SerializeField] private List<RoundConfig> rounds = new List<RoundConfig>();

    [Header("Input")]
    [SerializeField] private KeyCode dealKey = KeyCode.Space;

    private int _roundIndex = -1;

    public void DealNextRoundAndBeginPlay()
    {
        if (hand == null) return;
        if (rounds == null || rounds.Count == 0)
        {
            Debug.LogError($"{nameof(GameFlowController)}: rounds is empty.");
            return;
        }

        _roundIndex = (_roundIndex + 1) % rounds.Count;
        var round = rounds[_roundIndex];

        hand.SetRound(round);
        hand.DealCurrentRound();
        hand.SetInteractable(true);

        // UI 刷新
        if (hud != null)
            hud.SetRoundInfo(_roundIndex + 1, hand.RequiredPlayCount);
    }

    private void Update()
    {
        if (Input.GetKeyDown(dealKey))
            DealNextRoundAndBeginPlay();
    }
}