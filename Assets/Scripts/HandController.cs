using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class HandController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handRoot;

    [Header("Factory")]
    [SerializeField] private CardView cardPrefab;

    [Header("Current Round")]
    [SerializeField] private RoundConfig currentRound;

    [Header("Layout")]
    [SerializeField] private float cardSpacing = 0.6f;

    private readonly List<CardView> _hand = new();
    private readonly List<CardView> _selected = new();

    public bool CanInteract { get; private set; }

    public int RequiredPlayCount => currentRound != null ? currentRound.RequiredCount : 0;

    public void SetRound(RoundConfig round)
    {
        currentRound = round;
    }

    private void Awake()
    {
        AutoRegisterCardsUnderRoot();
        LayoutHand();
    }

    public void SetInteractable(bool canInteract)
    {
        CanInteract = canInteract;
        if (!CanInteract)
            ClearSelection();
    }

    public void ToggleSelect(CardView card)
    {
        if (!CanInteract) return;
        if (card == null) return;

        if (_selected.Contains(card))
        {
            _selected.Remove(card);
            card.SetSelected(false);
        }
        else
        {
            _selected.Add(card);
            card.SetSelected(true);
        }
    }

    // 供 UI Button 直接绑定调用
    public void PlaySelected()
    {
        if (!CanInteract) return;
        if (_selected.Count == 0) return;
        if (currentRound == null) return;

        if (!currentRound.IsSelectionCountValid(_selected.Count))
        {
            Debug.Log($"Wrong count: need {currentRound.RequiredCount}, but played {_selected.Count}.");
            return;
        }

        var playedIds = _selected.Select(c => c.CardId).ToArray();
        if (!currentRound.IsCorrect(playedIds))
        {
            Debug.Log("Wrong cards.");
            return;
        }

        Debug.Log("Correct!");

        foreach (var card in _selected.ToArray())
        {
            _hand.Remove(card);
            Destroy(card.gameObject);
        }

        _selected.Clear();
        LayoutHand();
    }

    public void ClearHand()
    {
        ClearSelection();

        foreach (var card in _hand.ToArray())
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        _hand.Clear();
        LayoutHand();
    }

    /// <summary>
    /// 按当前回合配置发固定手牌（RoundConfig.dealtCards）。
    /// </summary>
    public void DealCurrentRound()
    {
        if (currentRound == null)
        {
            Debug.LogError($"{nameof(HandController)}: currentRound is not assigned.");
            return;
        }

        DealCards(currentRound.DealtCards);
    }

    public void DealCards(IReadOnlyList<CardDefinition> definitions)
    {
        ClearHand();

        if (handRoot == null)
        {
            Debug.LogError($"{nameof(HandController)}: handRoot is not assigned.");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogError($"{nameof(HandController)}: cardPrefab is not assigned.");
            return;
        }

        if (definitions == null || definitions.Count == 0)
        {
            Debug.LogWarning($"{nameof(HandController)}: no cards to deal.");
            return;
        }

        for (int i = 0; i < definitions.Count; i++)
        {
            var def = definitions[i];
            var card = Instantiate(cardPrefab, handRoot);
            card.Initialize(this);
            card.SetDefinition(def);

            _hand.Add(card);
        }

        LayoutHand();
    }

    public void LayoutHand()
    {
        if (handRoot == null) return;

        float total = (_hand.Count - 1) * cardSpacing;
        float startX = -total * 0.5f;

        for (int i = 0; i < _hand.Count; i++)
        {
            var card = _hand[i];
            if (card == null) continue;

            var basePos = new Vector3(startX + i * cardSpacing, 0f, 0f);
            card.SetBaseLocalPosition(basePos);

            var targetPos = basePos + (card.IsSelected ? new Vector3(0f, 0.35f, 0f) : Vector3.zero);
            card.MoveToLocal(targetPos);

            card.transform.SetSiblingIndex(i);
        }
    }

    private void ClearSelection()
    {
        foreach (var c in _selected)
            if (c != null) c.SetSelected(false);

        _selected.Clear();
    }

    private void AutoRegisterCardsUnderRoot()
    {
        if (handRoot == null) return;

        _hand.Clear();

        foreach (Transform child in handRoot)
        {
            var card = child.GetComponent<CardView>();
            if (card == null) continue;

            card.Initialize(this);
            _hand.Add(card);
        }
    }
}